using DMCompiler.DM.Visitors;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM {
    internal sealed class DMProc {
        public class LocalVariable {
            public readonly int Id;
            public readonly bool IsParameter;
            public DreamPath? Type;

            public LocalVariable(int id, bool isParameter, DreamPath? type) {
                Id = id;
                IsParameter = isParameter;
                Type = type;
            }
        }

        public sealed class LocalConstVariable : LocalVariable {
            public readonly Expressions.Constant Value;

            public LocalConstVariable(int id, DreamPath? type, Expressions.Constant value) : base(id, false, type) {
                Value = value;
            }
        }

        private struct CodeLabelReference {
            public readonly string Identifier;
            public readonly string Placeholder;
            public readonly Location Location;
            public readonly DMProcScope Scope;

            public CodeLabelReference(string identifier, string placeholder, Location location, DMProcScope scope) {
                Identifier = identifier;
                Placeholder = placeholder;
                Scope = scope;
                Location = location;
            }
        }

        public class CodeLabel {
            public readonly int Id;
            public readonly string Name;
            public readonly long ByteOffset;

            public int ReferencedCount = 0;

            public string LabelName => $"{Name}_{Id}_codelabel";

            private static int _idCounter = 0;

            public CodeLabel(string name, long offset) {
                Id = _idCounter++;
                Name = name;
                ByteOffset = offset;
            }
        }

        private class DMProcScope {
            public readonly Dictionary<string, LocalVariable> LocalVariables = new();
            public readonly Dictionary<string, CodeLabel> LocalCodeLabels = new();
            public readonly DMProcScope? ParentScope;

            public DMProcScope() { }

            public DMProcScope(DMProcScope? parentScope) {
                ParentScope = parentScope;
            }
        }

        public MemoryStream Bytecode = new();
        public List<string> Parameters = new();
        public List<DMValueType> ParameterTypes = new();
        public Location Location;
        public ProcAttributes Attributes;
        public bool IsVerb = false;
        public string Name => _astDefinition?.Name ?? "<init>";
        public int Id;
        public Dictionary<string, int> GlobalVariables = new();

        public string? VerbName;
        public string? VerbCategory = string.Empty;
        public string? VerbDesc;
        public sbyte Invisibility;

        private DMObject _dmObject;
        private DMASTProcDefinition? _astDefinition;
        private BinaryWriter _bytecodeWriter;
        private Stack<CodeLabelReference> _pendingLabelReferences = new();
        private Dictionary<string, long> _labels = new();
        private List<(long Position, string LabelName)> _unresolvedLabels = new();
        private Stack<string>? _loopStack = null;
        private Stack<DMProcScope> _scopes = new();
        private Dictionary<string, LocalVariable> _parameters = new();
        private int _labelIdCounter;
        private int _maxStackSize;
        private int _currentStackSize;
        private bool _negativeStackSizeError;

        private List<LocalVariableJson> _localVariableNames = new();
        private int _localVariableIdCounter;

        private readonly List<SourceInfoJson> _sourceInfo = new();
        private string? _lastSourceFile;

        private int AllocLocalVariable(string name) {
            _localVariableNames.Add(new LocalVariableJson { Offset = (int)Bytecode.Position, Add = name });
            return _localVariableIdCounter++;
        }

        private void DeallocLocalVariables(int amount) {
            if (amount > 0) {
                _localVariableNames.Add(new LocalVariableJson { Offset = (int)Bytecode.Position, Remove = amount });
                _localVariableIdCounter -= amount;
            }
        }

        public DMProc(int id, DMObject dmObject, DMASTProcDefinition? astDefinition) {
            Id = id;
            _dmObject = dmObject;
            _astDefinition = astDefinition;
            if (_astDefinition?.IsOverride ?? false) Attributes |= ProcAttributes.IsOverride; // init procs don't have AST definitions
            Location = astDefinition?.Location ?? Location.Unknown;
            _bytecodeWriter = new BinaryWriter(Bytecode);
            _scopes.Push(new DMProcScope());
        }

        public void Compile() {
            DMCompiler.VerbosePrint($"Compiling proc {_dmObject?.Path.ToString() ?? "Unknown"}.{Name}()");

            if (_astDefinition is not null) { // It's null for initialization procs
                foreach (DMASTDefinitionParameter parameter in _astDefinition.Parameters) {
                    AddParameter(parameter.Name, parameter.Type, parameter.ObjectType);
                }

                new DMProcBuilder(_dmObject, this).ProcessProcDefinition(_astDefinition);
            }
        }

        public ProcDefinitionJson GetJsonRepresentation() {
            ProcDefinitionJson procDefinition = new ProcDefinitionJson();

            procDefinition.OwningTypeId = _dmObject.Id;
            procDefinition.Name = Name;
            procDefinition.IsVerb = IsVerb;
            procDefinition.SourceInfo = _sourceInfo;

            if ((Attributes & ProcAttributes.None) != ProcAttributes.None) {
                procDefinition.Attributes = Attributes;
            }

            procDefinition.VerbName = VerbName;
            // Normally VerbCategory is "" by default and null to hide it, but we invert those during (de)serialization to reduce JSON size
            VerbCategory = VerbCategory switch {
                "" => null,
                null => string.Empty,
                _ => VerbCategory
            };
            procDefinition.VerbCategory = VerbCategory;
            procDefinition.VerbDesc = VerbDesc;
            procDefinition.Invisibility = Invisibility;

            procDefinition.MaxStackSize = _maxStackSize;

            if (Bytecode.Length > 0) procDefinition.Bytecode = Bytecode.ToArray();
            if (Parameters.Count > 0) {
                procDefinition.Arguments = new List<ProcArgumentJson>();

                for (int i = 0; i < Parameters.Count; i++) {
                    string argumentName = Parameters[i];
                    DMValueType argumentType = ParameterTypes[i];

                    procDefinition.Arguments.Add(new ProcArgumentJson() {
                        Name = argumentName,
                        Type = argumentType
                    });
                }
            }

            if (_localVariableNames.Count > 0) {
                procDefinition.Locals = _localVariableNames;
            }

            return procDefinition;
        }

        public string GetLocalVarName(int index) {
            return _localVariableNames[index].Add;
        }

        public void WaitFor(bool waitFor) {
            if (waitFor) {
                // "waitfor" is true by default
                Attributes &= ~ProcAttributes.DisableWaitfor;
            } else {
                Attributes |= ProcAttributes.DisableWaitfor;
            }
        }

        public DMVariable CreateGlobalVariable(DreamPath? type, string name, bool isConst, out int id) {
            id = DMObjectTree.CreateGlobal(out DMVariable global, type, name, isConst);

            GlobalVariables[name] = id;
            return global;
        }

        public int? GetGlobalVariableId(string name) {
            if (GlobalVariables.TryGetValue(name, out int id)) {
                return id;
            }

            return null;
        }

        public void AddParameter(string name, DMValueType valueType, DreamPath? type) {
            Parameters.Add(name);
            ParameterTypes.Add(valueType);

            if (_parameters.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, _astDefinition.Location, $"Duplicate argument \"{name}\"");
            } else {
                _parameters.Add(name, new LocalVariable(_parameters.Count, true, type));
            }
        }

        public void ResolveCodeLabelReferences() {
            while(_pendingLabelReferences.Count > 0) {
                CodeLabelReference reference = _pendingLabelReferences.Pop();
                CodeLabel? label = GetCodeLabel(reference.Identifier, reference.Scope);

                // Failed to find the label in the given context
                if(label == null) {
                    DMCompiler.Emit(
                        WarningCode.ItemDoesntExist,
                        reference.Location,
                        $"Label \"{reference.Identifier}\" unreachable from scope or does not exist"
                    );
                    // Not cleaning away the placeholder will emit another compiler error
                    // let's not do that
                    _unresolvedLabels.RemoveAt(
                        _unresolvedLabels.FindIndex(((long Position, string LabelName)o) => o.LabelName == reference.Placeholder)
                    );
                    continue;
                }

                // Found it.
                _labels.Add(reference.Placeholder, label.ByteOffset);
                label.ReferencedCount += 1;

                // I was thinking about going through to replace all the placeholers
                // with the actual label.LabelName, but it means I need to modify
                // _unresolvedLabels, being a list of tuple objects. Fuck that noise
            }

            // TODO: Implement "unused label" like in BYOND DM, use label.ReferencedCount to figure out
            // foreach (CodeLabel codeLabel in CodeLabels) {
            //  ...
            // }
        }

        public void ResolveLabels() {
            ResolveCodeLabelReferences();

            foreach ((long Position, string LabelName) unresolvedLabel in _unresolvedLabels) {
                if (_labels.TryGetValue(unresolvedLabel.LabelName, out long labelPosition)) {
                    _bytecodeWriter.Seek((int)unresolvedLabel.Position, SeekOrigin.Begin);
                    WriteInt((int)labelPosition);
                } else {
                    DMCompiler.Emit(WarningCode.BadLabel, Location, "Label \"" + unresolvedLabel.LabelName + "\" could not be resolved");
                }
            }

            _unresolvedLabels.Clear();
            _bytecodeWriter.Seek(0, SeekOrigin.End);
        }

        public string MakePlaceholderLabel() => $"PLACEHOLDER_{_pendingLabelReferences.Count}_LABEL";

        public CodeLabel? TryAddCodeLabel(string name) {
            if (_scopes.Peek().LocalCodeLabels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location, $"A label with the name \"{name}\" already exists");
                return null;
            }

            CodeLabel label = new CodeLabel(name, Bytecode.Position);
            _scopes.Peek().LocalCodeLabels.Add(name, label);
            return label;
        }

        private CodeLabel? GetCodeLabel(string name, DMProcScope? scope = null) {
            DMProcScope? _scope = scope ?? _scopes.Peek();
            while (_scope != null) {
                if (_scope.LocalCodeLabels.TryGetValue(name, out var localCodeLabel))
                    return localCodeLabel;

                _scope = _scope.ParentScope;
            }
            return null;
        }

        public void AddLabel(string name) {
            if (_labels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location, $"A label with the name \"{name}\" already exists");
                return;
            }

            _labels.Add(name, Bytecode.Position);
        }

        public bool TryAddLocalVariable(string name, DreamPath? type) {
            if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
                return false;

            int localVarId = AllocLocalVariable(name);
            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalVariable(localVarId, false, type));
        }

        public bool TryAddLocalConstVariable(string name, DreamPath? type, Expressions.Constant value) {
            if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
                return false;

            int localVarId = AllocLocalVariable(name);
            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalConstVariable(localVarId, type, value));
        }

        public LocalVariable? GetLocalVariable(string name) {
            if (_parameters.TryGetValue(name, out var parameter)) {
                return parameter;
            }

            DMProcScope? scope = _scopes.Peek();
            while (scope != null) {
                if (scope.LocalVariables.TryGetValue(name, out var localVariable))
                    return localVariable;

                scope = scope.ParentScope;
            }

            return null;
        }

        public DMReference GetLocalVariableReference(string name) {
            LocalVariable? local = GetLocalVariable(name);

            return local.IsParameter ? DMReference.CreateArgument(local.Id) : DMReference.CreateLocal(local.Id);
        }

        public void Error() {
            WriteOpcode(DreamProcOpcode.Error);
        }

        public void DebugSource(Location location) {
            var sourceInfo = new SourceInfoJson() {
                Offset = (int)_bytecodeWriter.BaseStream.Position,
                Line = location.Line ?? -1
            };

            var sourceFile = location.SourceFile.Replace('\\', '/');

            // Only write the source file if it has changed
            if (_lastSourceFile != sourceFile) {
                sourceInfo.File = DMObjectTree.AddString(sourceFile);
            } else if (_sourceInfo.Count > 0 && sourceInfo.Line == _sourceInfo[^1].Line) {
                // Don't need to write this source info if it's the same source & line as the last
                return;
            }

            _lastSourceFile = sourceFile;
            _sourceInfo.Add(sourceInfo);
        }

        public void PushReferenceValue(DMReference reference) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushReferenceValue);
            WriteReference(reference);
        }

        public void CreateListEnumerator() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        }

        public void CreateFilteredListEnumerator(DreamPath filterType) {
            if (!DMObjectTree.TryGetTypeId(filterType, out var filterTypeId)) {
                DMCompiler.ForcedError($"Cannot filter enumeration by type {filterType}");
            }

            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CreateFilteredListEnumerator);
            WriteInt(filterTypeId);
        }

        public void CreateTypeEnumerator() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CreateTypeEnumerator);
        }

        public void CreateRangeEnumerator() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.CreateRangeEnumerator);
        }

        public void Enumerate(DMReference reference) {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                WriteOpcode(DreamProcOpcode.Enumerate);
                WriteReference(reference);
                WriteLabel($"{peek}_end");
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void EnumerateNoAssign() {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                WriteOpcode(DreamProcOpcode.EnumerateNoAssign);
                WriteLabel($"{peek}_end");
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void DestroyEnumerator() {
            WriteOpcode(DreamProcOpcode.DestroyEnumerator);
        }

        public void CreateList(int size) {
            ShrinkStack(size - 1); //Shrinks by the size of the list, grows by 1
            WriteOpcode(DreamProcOpcode.CreateList);
            WriteInt(size);
        }

        public void CreateAssociativeList(int size) {
            ShrinkStack(size * 2 - 1); //Shrinks by twice the size of the list, grows by 1
            WriteOpcode(DreamProcOpcode.CreateAssociativeList);
            WriteInt(size);
        }

        public string NewLabelName() {
            return "label" + _labelIdCounter++;
        }

        public void LoopStart(string loopLabel) {
            _loopStack ??= new Stack<string>(3); // Start, continue, end
            _loopStack.Push(loopLabel);

            AddLabel(loopLabel + "_start");
            StartScope();
        }

        public void MarkLoopContinue(string loopLabel) {
            AddLabel($"{loopLabel}_continue");
        }

        public void BackgroundSleep() {
            // TODO This seems like a bad way to handle background, doesn't it?

            if ((Attributes & ProcAttributes.Background) == ProcAttributes.Background) {
                if (!DMObjectTree.TryGetGlobalProc("sleep", out var sleepProc)) {
                    throw new CompileErrorException(Location, "Cannot do a background sleep without a sleep proc");
                }

                PushFloat(-1); // argument given to sleep()
                Call(DMReference.CreateGlobalProc(sleepProc.Id), DMCallArgumentsType.FromStack, 1);
                Pop(); // Pop the result of the sleep call
            }
        }

        public void LoopJumpToStart(string loopLabel) {
            BackgroundSleep();
            Jump($"{loopLabel}_start");
        }

        public void LoopEnd() {
            if (_loopStack?.TryPop(out var pop) ?? false) {
                AddLabel(pop + "_end");
            } else {
                DMCompiler.ForcedError(Location, "Cannot pop empty loop stack");
            }
            EndScope();
        }

        public void SwitchCase(string caseLabel) {
            ShrinkStack(1); //This could either shrink the stack by 1 or 2. Assume 1.
            WriteOpcode(DreamProcOpcode.SwitchCase);
            WriteLabel(caseLabel);
        }

        public void SwitchCaseRange(string caseLabel) {
            ShrinkStack(2); //This could either shrink the stack by 2 or 3. Assume 2.
            WriteOpcode(DreamProcOpcode.SwitchCaseRange);
            WriteLabel(caseLabel);
        }

        public void Browse() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.Browse);
        }

        public void BrowseResource() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.BrowseResource);
        }

        public void OutputControl() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.OutputControl);
        }

        public void Ftp() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.Ftp);
        }

        public void OutputReference(DMReference leftRef) {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.OutputReference);
            WriteReference(leftRef);
        }

        public void Output() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.Output);
        }

        public void Input(DMReference leftRef, DMReference rightRef) {
            WriteOpcode(DreamProcOpcode.Input);
            WriteReference(leftRef);
            WriteReference(rightRef);
        }

        public void Spawn(string jumpTo) {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Spawn);
            WriteLabel(jumpTo);
        }

        public void Break(DMASTIdentifier? label = null) {
            if (label is not null) {
                var codeLabel = (
                    GetCodeLabel(label.Identifier)?.LabelName ??
                    label.Identifier + "_codelabel"
                );
                if (!_labels.ContainsKey(codeLabel)) {
                    DMCompiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
                }
                Jump(codeLabel + "_end");
            } else if (_loopStack?.TryPeek(out var peek) ?? false) {
                Jump(peek + "_end");
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void BreakIfFalse() {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                JumpIfFalse($"{peek}_end");
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void Continue(DMASTIdentifier? label = null) {
            // TODO: Clean up this godawful label handling
            if (label is not null) {
                // Also, labelled loops always need the label declared first, so stick it like this way
                var codeLabel = (
                    GetCodeLabel(label.Identifier)?.LabelName ??
                    label.Identifier + "_codelabel"
                );
                if (!_labels.ContainsKey(codeLabel)) {
                    DMCompiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
                }

                var labelList = _labels.Keys.ToList();
                var continueLabel = string.Empty;
                for (var i = labelList.IndexOf(codeLabel) + 1; i < labelList.Count; i++) {
                    if (labelList[i].EndsWith("_start")) {
                        continueLabel = labelList[i].Replace("_start", "_continue");
                        break;
                    }
                }

                BackgroundSleep();
                Jump(continueLabel);
            } else {
                BackgroundSleep();

                if (_loopStack?.TryPeek(out var peek) ?? false) {
                    Jump(peek + "_continue");
                } else {
                    DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
                }
            }
        }

        public void Goto(DMASTIdentifier label) {
            var placeholder = MakePlaceholderLabel();
            _pendingLabelReferences.Push(new CodeLabelReference(
                label.Identifier,
                placeholder,
                label.Location,
                _scopes.Peek()
            ));
            Jump(placeholder);
        }

        public void Pop() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Pop);
        }

        public void PopReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.PopReference);
            WriteReference(reference, false);
        }

        public void BooleanOr(string endLabel) {
            ShrinkStack(1); //Either shrinks the stack 1 or 0. Assume 1.
            WriteOpcode(DreamProcOpcode.BooleanOr);
            WriteLabel(endLabel);
        }

        public void BooleanAnd(string endLabel) {
            ShrinkStack(1); //Either shrinks the stack 1 or 0. Assume 1.
            WriteOpcode(DreamProcOpcode.BooleanAnd);
            WriteLabel(endLabel);
        }

        public void StartScope() {
            _scopes.Push(new DMProcScope(_scopes.Peek()));
        }

        public void EndScope() {
            DMProcScope destroyedScope = _scopes.Pop();
            DeallocLocalVariables(destroyedScope.LocalVariables.Count);
        }

        public void Jump(string label) {
            WriteOpcode(DreamProcOpcode.Jump);
            WriteLabel(label);
        }

        public void JumpIfFalse(string label) {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.JumpIfFalse);
            WriteLabel(label);
        }

        public void JumpIfTrue(string label) {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.JumpIfTrue);
            WriteLabel(label);
        }

        //Jumps to the label and pushes null if the reference is dereferencing null
        public void JumpIfNullDereference(DMReference reference, string label) {
            //Either grows the stack by 0 or 1. Assume 0.
            WriteOpcode(DreamProcOpcode.JumpIfNullDereference);
            WriteReference(reference, affectStack: false);
            WriteLabel(label);
        }

        public void JumpIfNull(string label) {
            // Conditionally pops one value
            WriteOpcode(DreamProcOpcode.JumpIfNull);
            WriteLabel(label);
        }

        public void JumpIfNullNoPop(string label) {
            WriteOpcode(DreamProcOpcode.JumpIfNullNoPop);
            WriteLabel(label);
        }

        public void JumpIfTrueReference(DMReference reference, string label) {
            WriteOpcode(DreamProcOpcode.JumpIfTrueReference);
            WriteReference(reference, affectStack: false);
            WriteLabel(label);
        }

        public void JumpIfFalseReference(DMReference reference, string label) {
            WriteOpcode(DreamProcOpcode.JumpIfFalseReference);
            WriteReference(reference, affectStack: false);
            WriteLabel(label);
        }

        public void DereferenceField(string field) {
            WriteOpcode(DreamProcOpcode.DereferenceField);
            WriteString(field);
        }

        public void DereferenceIndex() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.DereferenceIndex);
        }

        public void DereferenceCall(string field, DMCallArgumentsType argumentsType, int argumentStackSize) {
            ShrinkStack(argumentStackSize); // Pops proc owner and arguments, pushes result
            WriteOpcode(DreamProcOpcode.DereferenceCall);
            WriteString(field);
            WriteByte((byte)argumentsType);
            WriteInt(argumentStackSize);
        }

        public void Call(DMReference reference, DMCallArgumentsType argumentsType, int argumentStackSize) {
            ShrinkStack(argumentStackSize - 1); // Pops all arguments, pushes return value
            WriteOpcode(DreamProcOpcode.Call);
            WriteReference(reference);
            WriteByte((byte)argumentsType);
            WriteInt(argumentStackSize);
        }

        public void CallStatement(DMCallArgumentsType argumentsType, int argumentStackSize) {
            //Shrinks the stack by argumentStackSize. Could also shrink it by argumentStackSize+1, but assume not.
            ShrinkStack(argumentStackSize);
            WriteOpcode(DreamProcOpcode.CallStatement);
            WriteByte((byte)argumentsType);
            WriteInt(argumentStackSize);
        }

        public void Prompt(DMValueType types) {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.Prompt);
            WriteInt((int)types);
        }

        public void Initial() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Initial);
        }

        public void Return() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Return);
        }

        public void Throw() {
            WriteOpcode(DreamProcOpcode.Throw);
        }

        public void Assign(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Assign);
            WriteReference(reference);
        }
        public void AssignInto(DMReference reference) {
            WriteOpcode(DreamProcOpcode.AssignInto);
            WriteReference(reference);
        }

        public void CreateObject(DMCallArgumentsType argumentsType, int argumentStackSize) {
            ShrinkStack(argumentStackSize); // Pops type and arguments, pushes new object
            WriteOpcode(DreamProcOpcode.CreateObject);
            WriteByte((byte)argumentsType);
            WriteInt(argumentStackSize);
        }

        public void DeleteObject() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.DeleteObject);
        }

        public void Not() {
            WriteOpcode(DreamProcOpcode.BooleanNot);
        }

        public void Negate() {
            WriteOpcode(DreamProcOpcode.Negate);
        }

        public void Add() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Add);
        }

        public void Subtract() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Subtract);
        }

        public void Multiply() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Multiply);
        }

        public void MultiplyReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.MultiplyReference);
            WriteReference(reference);
        }

        public void Divide() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Divide);
        }

        public void DivideReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.DivideReference);
            WriteReference(reference);
        }

        public void Modulus() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Modulus);
        }

        public void ModulusModulus() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.ModulusModulus);
        }

        public void ModulusReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.ModulusReference);
            WriteReference(reference);
        }

        public void ModulusModulusReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.ModulusModulusReference);
            WriteReference(reference);
        }

        public void Power() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Power);
        }

        public void Append(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Append);
            WriteReference(reference);
        }

        public void Increment(DMReference reference) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.Increment);
            WriteReference(reference);
        }

        public void Decrement(DMReference reference) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.Decrement);
            WriteReference(reference);
        }

        public void Remove(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Remove);
            WriteReference(reference);
        }

        public void Combine(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Combine);
            WriteReference(reference);
        }

        public void Mask(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Mask);
            WriteReference(reference);
        }

        public void BitShiftLeft() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitShiftLeft);
        }

        public void BitShiftLeftReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.BitShiftLeftReference);
            WriteReference(reference);
        }

        public void BitShiftRight() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitShiftRight);
        }

        public void BitShiftRightReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.BitShiftRightReference);
            WriteReference(reference);
        }

        public void BinaryNot() {
            WriteOpcode(DreamProcOpcode.BitNot);
        }

        public void BinaryAnd() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitAnd);
        }

        public void BinaryXor() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitXor);
        }

        public void BinaryXorReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.BitXorReference);
            WriteReference(reference);
        }

        public void BinaryOr() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitOr);
        }

        public void Equal() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareEquals);
        }

        public void NotEqual() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareNotEquals);
        }

        public void Equivalent() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareEquivalent);
        }

        public void NotEquivalent() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareNotEquivalent);
        }

        public void GreaterThan() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareGreaterThan);
        }

        public void GreaterThanOrEqual() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareGreaterThanOrEqual);
        }

        public void LessThan() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareLessThan);
        }

        public void LessThanOrEqual() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CompareLessThanOrEqual);
        }

        public void Sin() {
            WriteOpcode(DreamProcOpcode.Sin);
        }

        public void Cos() {
            WriteOpcode(DreamProcOpcode.Cos);
        }

        public void Tan() {
            WriteOpcode(DreamProcOpcode.Tan);
        }

        public void ArcSin() {
            WriteOpcode(DreamProcOpcode.ArcSin);
        }

        public void ArcCos() {
            WriteOpcode(DreamProcOpcode.ArcCos);
        }

        public void ArcTan() {
            WriteOpcode(DreamProcOpcode.ArcTan);
        }

        public void ArcTan2() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.ArcTan2);
        }

        public void Sqrt() {
            WriteOpcode(DreamProcOpcode.Sqrt);
        }

        public void Log() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Log);
        }

        public void LogE() {
            WriteOpcode(DreamProcOpcode.LogE);
        }

        public void Abs() {
            WriteOpcode(DreamProcOpcode.Abs);
        }

        public void PushFloat(float value) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushFloat);
            WriteFloat(value);
        }

        public void PushString(string value) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushString);
            WriteString(value);
        }

        public void PushResource(string value) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushResource);
            WriteString(value);
        }

        public void PushType(int typeId) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushType);
            WriteInt(typeId);
        }

        public void PushProc(int procId) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushProc);
            WriteInt(procId);
        }

        public void PushNull() {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushNull);
        }

        public void PushGlobalVars() {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushGlobalVars);
        }

        public void FormatString(string value) {
            int formatCount = 0;
            for (int i = 0; i < value.Length; i++) {
                if (StringFormatEncoder.Decode(value[i], out var formatType))
                {
                    if(StringFormatEncoder.IsInterpolation(formatType.Value))
                        formatCount++;
                }
            }

            ShrinkStack(formatCount - 1); //Shrinks by the amount of formats in the string, grows 1
            WriteOpcode(DreamProcOpcode.FormatString);
            WriteString(value);
            WriteInt(formatCount);
        }

        public void IsInList() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.IsInList);
        }

        public void IsInRange() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.IsInRange);
        }

        public void IsSaved() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.IsSaved);
        }

        public void IsType() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.IsType);
        }

        public void IsNull() {
            WriteOpcode(DreamProcOpcode.IsNull);
        }

        public void Length() {
            WriteOpcode(DreamProcOpcode.Length);
        }

        public void GetStep() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.GetStep);
        }

        public void GetDir() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.GetDir);
        }

        public void LocateCoordinates() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.LocateCoord);
        }

        public void Gradient(DMCallArgumentsType argumentsType, int argumentStackSize) {
            ShrinkStack(argumentStackSize - 1); // Pops arguments, pushes gradient result
            WriteOpcode(DreamProcOpcode.Gradient);
            WriteByte((byte)argumentsType);
            WriteInt(argumentStackSize);
        }

        public void PickWeighted(int count) {
            ShrinkStack(count * 2 - 1);
            WriteOpcode(DreamProcOpcode.PickWeighted);
            WriteInt(count);
        }

        public void PickUnweighted(int count) {
            ShrinkStack(count - 1);
            WriteOpcode(DreamProcOpcode.PickUnweighted);
            WriteInt(count);
        }

        public void Prob() {
            //Pops 1, pushes 1
            WriteOpcode(DreamProcOpcode.Prob);
        }

        public void MassConcatenation(int count) {
            ShrinkStack(count - 1);
            WriteOpcode(DreamProcOpcode.MassConcatenation);
            WriteInt(count);
        }

        public void Locate() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Locate);
        }

        public void StartTry(string label, DMReference reference) {
            WriteOpcode(DreamProcOpcode.Try);
            WriteLabel(label);
            WriteReference(reference);
        }

        public void StartTryNoValue(string label) {
            WriteOpcode(DreamProcOpcode.TryNoValue);
            WriteLabel(label);
        }

        public void EndTry() {
            WriteOpcode(DreamProcOpcode.EndTry);
        }

        private void WriteOpcode(DreamProcOpcode opcode) {
            _bytecodeWriter.Write((byte)opcode);
        }

        private void WriteByte(byte value) {
            _bytecodeWriter.Write(value);
        }

        private void WriteInt(int value) {
            _bytecodeWriter.Write(value);
        }

        private void WriteFloat(float value) {
            _bytecodeWriter.Write(value);
        }

        private void WriteString(string value) {
            int stringId = DMObjectTree.AddString(value);

            WriteInt(stringId);
        }

        private void WriteLabel(string labelName) {
            _unresolvedLabels.Add((Bytecode.Position, labelName));
            WriteInt(0); //Resolved later
        }

        private void WriteReference(DMReference reference, bool affectStack = true) {
            WriteByte((byte)reference.RefType);

            switch (reference.RefType) {
                case DMReference.Type.Argument:
                case DMReference.Type.Local:
                    WriteByte((byte)reference.Index);
                    break;

                case DMReference.Type.GlobalProc:
                case DMReference.Type.Global:
                    WriteInt(reference.Index);
                    break;

                case DMReference.Type.Field:
                    WriteString(reference.Name);
                    ShrinkStack(affectStack ? 1 : 0);
                    break;

                case DMReference.Type.SrcField:
                case DMReference.Type.SrcProc:
                    WriteString(reference.Name);
                    break;

                case DMReference.Type.ListIndex:
                    ShrinkStack(affectStack ? 2 : 0);
                    break;

                case DMReference.Type.SuperProc:
                case DMReference.Type.Src:
                case DMReference.Type.Self:
                case DMReference.Type.Args:
                case DMReference.Type.Usr:
                    break;

                default:
                    throw new CompileAbortException(Location, $"Invalid reference type {reference.RefType}");
            }
        }

        private void GrowStack(int size) {
            _currentStackSize += size;
            _maxStackSize = Math.Max(_currentStackSize, _maxStackSize);
        }

        private void ShrinkStack(int size) {
            _currentStackSize -= size;
            _maxStackSize = Math.Max(_currentStackSize, _maxStackSize);
            if (_currentStackSize < 0 && !_negativeStackSizeError) {
                _negativeStackSizeError = true;
                DMCompiler.ForcedError(Location, "Negative stack size");
            }
        }
    }
}
