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
using DMCompiler.DM.Optimizer;
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

        public struct CodeLabelReference {
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

        internal class DMProcScope {
            public readonly Dictionary<string, LocalVariable> LocalVariables = new();
            public readonly Dictionary<string, CodeLabel> LocalCodeLabels = new();
            public readonly DMProcScope? ParentScope;

            public DMProcScope() {
            }

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
        private AnnotatedByteCodeWriter _annotatedBytecodeWriter = new();
        private Stack<CodeLabelReference> _pendingLabelReferences = new();
        private Dictionary<string, long> _labels = new();
        private Stack<string>? _loopStack = null;
        private Stack<DMProcScope> _scopes = new();
        private Dictionary<string, LocalVariable> _parameters = new();
        private int _labelIdCounter;

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
            if (_astDefinition?.IsOverride ?? false)
                Attributes |= ProcAttributes.IsOverride; // init procs don't have AST definitions
            Location = astDefinition?.Location ?? Location.Unknown;
            _annotatedBytecodeWriter = new AnnotatedByteCodeWriter(Bytecode);
            _scopes.Push(new DMProcScope());
        }


        public DreamPath GetPath() {
            return _dmObject.Path;
        }

        public void Compile() {
            DMCompiler.VerbosePrint($"Compiling proc {_dmObject?.Path.ToString() ?? "Unknown"}.{Name}()");

            if (_astDefinition is not null) {
                // It's null for initialization procs
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

            procDefinition.MaxStackSize = _annotatedBytecodeWriter.GetMaxStackSize();

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
                DMCompiler.Emit(WarningCode.DuplicateVariable, _astDefinition.Location,
                    $"Duplicate argument \"{name}\"");
            } else {
                _parameters.Add(name, new LocalVariable(_parameters.Count, true, type));
            }
        }

        public string MakePlaceholderLabel() => $"PLACEHOLDER_{_pendingLabelReferences.Count}_LABEL";

        public CodeLabel? TryAddCodeLabel(string name) {
            if (_scopes.Peek().LocalCodeLabels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location,
                    $"A label with the name \"{name}\" already exists");
                return null;
            }

            CodeLabel label = new CodeLabel(name, Bytecode.Position);
            _scopes.Peek().LocalCodeLabels.Add(name, label);
            return label;
        }

        public void AddLabel(string name) {
            if (_labels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location,
                    $"A label with the name \"{name}\" already exists");
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
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Error, Location);
        }

        public void DebugSource(Location location) {
            var sourceInfo = new SourceInfoJson() {
                Offset = (int)_annotatedBytecodeWriter.BaseStream.Position,
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
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushReferenceValue, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void CreateListEnumerator() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateListEnumerator, Location);
        }

        public void CreateFilteredListEnumerator(DreamPath filterType) {
            if (!DMObjectTree.TryGetTypeId(filterType, out var filterTypeId)) {
                DMCompiler.ForcedError($"Cannot filter enumeration by type {filterType}");
            }

            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateFilteredListEnumerator, Location);
            _annotatedBytecodeWriter.WriteFilterID(filterTypeId, filterType, Location);
        }

        public void CreateTypeEnumerator() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateTypeEnumerator, Location);
        }

        public void CreateRangeEnumerator() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateRangeEnumerator, Location);
        }

        public void Enumerate(DMReference reference) {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Enumerate, Location);
                _annotatedBytecodeWriter.WriteReference(reference, Location);
                _annotatedBytecodeWriter.WriteLabel($"{peek}_end", Location);
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void EnumerateNoAssign() {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.EnumerateNoAssign, Location);
                _annotatedBytecodeWriter.WriteLabel($"{peek}_end", Location);
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void DestroyEnumerator() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DestroyEnumerator, Location);
        }

        public void CreateList(int size) {
            _annotatedBytecodeWriter.ResizeStack(-(size - 1)); //Shrinks by the size of the list, grows by 1
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateList, Location);
            _annotatedBytecodeWriter.WriteListSize(size, Location);
        }

        public void CreateAssociativeList(int size) {
            _annotatedBytecodeWriter.ResizeStack(-(size * 2 - 1)); //Shrinks by twice the size of the list, grows by 1
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateAssociativeList, Location);
            _annotatedBytecodeWriter.WriteListSize(size, Location);
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
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.SwitchCase, Location);
            _annotatedBytecodeWriter.WriteLabel(caseLabel, Location);
        }

        public void SwitchCaseRange(string caseLabel) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.SwitchCaseRange, Location);
            _annotatedBytecodeWriter.WriteLabel(caseLabel, Location);
        }

        public void Browse() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Browse, Location);
        }

        public void BrowseResource() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BrowseResource, Location);
        }

        public void OutputControl() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.OutputControl, Location);
        }

        public void Ftp() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Ftp, Location);
        }

        public void OutputReference(DMReference leftRef) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.OutputReference, Location);
            _annotatedBytecodeWriter.WriteReference(leftRef, Location);
        }

        public void Output() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Output, Location);
        }

        public void Input(DMReference leftRef, DMReference rightRef) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Input, Location);
            _annotatedBytecodeWriter.WriteReference(leftRef, Location);
            _annotatedBytecodeWriter.WriteReference(rightRef, Location);
        }

        public void Spawn(string jumpTo) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Spawn, Location);
            _annotatedBytecodeWriter.WriteLabel(jumpTo, Location);
        }

        public void Break(DMASTIdentifier? label = null) {
            if (label is not null) {
                var codeLabel = (_annotatedBytecodeWriter.GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
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
                    _annotatedBytecodeWriter.GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
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
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Pop, Location);
        }

        public void PopReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PopReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location, false);
        }

        public void BooleanOr(string endLabel) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BooleanOr, Location);
            _annotatedBytecodeWriter.WriteLabel(endLabel, Location);
        }

        public void BooleanAnd(string endLabel) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BooleanAnd, Location);
            _annotatedBytecodeWriter.WriteLabel(endLabel, Location);
        }

        public void StartScope() {
            _scopes.Push(new DMProcScope(_scopes.Peek()));
        }

        public void EndScope() {
            DMProcScope destroyedScope = _scopes.Pop();
            DeallocLocalVariables(destroyedScope.LocalVariables.Count);
        }

        public void Jump(string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Jump, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfFalse(string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfFalse, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfTrue(string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfTrue, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        //Jumps to the label and pushes null if the reference is dereferencing null
        public void JumpIfNullDereference(DMReference reference, string label) {
            //Either grows the stack by 0 or 1. Assume 0.
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfNullDereference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location, affectStack: false);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfNull(string label) {
            // Conditionally pops one value
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfNull, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfNullNoPop(string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfNullNoPop, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfTrueReference(DMReference reference, string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfTrueReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location, affectStack: false);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void JumpIfFalseReference(DMReference reference, string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.JumpIfFalseReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location, affectStack: false);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void DereferenceField(string field) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DereferenceField, Location);
            _annotatedBytecodeWriter.WriteString(field, Location);
        }

        public void DereferenceIndex() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DereferenceIndex, Location);
        }

        public void DereferenceCall(string field, DMCallArgumentsType argumentsType, int argumentStackSize) {
            _annotatedBytecodeWriter.ResizeStack(-argumentStackSize); // Pops proc owner and arguments, pushes result
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DereferenceCall, Location);
            _annotatedBytecodeWriter.WriteString(field, Location);
            _annotatedBytecodeWriter.WriteArgumentType(argumentsType, Location);
            _annotatedBytecodeWriter.WriteStackDelta(argumentStackSize, Location);
        }

        public void Call(DMReference reference, DMCallArgumentsType argumentsType, int argumentStackSize) {
            _annotatedBytecodeWriter.ResizeStack(-(argumentStackSize - 1)); // Pops all arguments, pushes return value
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Call, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
            _annotatedBytecodeWriter.WriteArgumentType(argumentsType, Location);
            _annotatedBytecodeWriter.WriteStackDelta(argumentStackSize, Location);
        }

        public void CallStatement(DMCallArgumentsType argumentsType, int argumentStackSize) {
            //Shrinks the stack by argumentStackSize. Could also shrink it by argumentStackSize+1, but assume not.
            _annotatedBytecodeWriter.ResizeStack(-argumentStackSize);
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CallStatement, Location);
            _annotatedBytecodeWriter.WriteArgumentType(argumentsType, Location);
            _annotatedBytecodeWriter.WriteStackDelta(argumentStackSize, Location);
        }

        public void Prompt(DMValueType types) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Prompt, Location);
            _annotatedBytecodeWriter.WriteType(types, Location);
        }

        public void Initial() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Initial, Location);
        }

        public void Return() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Return, Location);
        }

        public void Throw() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Throw, Location);
        }

        public void Assign(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Assign, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void AssignInto(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.AssignInto, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void CreateObject(DMCallArgumentsType argumentsType, int argumentStackSize) {
            _annotatedBytecodeWriter.ResizeStack(-argumentStackSize); // Pops type and arguments, pushes new object
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CreateObject, Location);
            _annotatedBytecodeWriter.WriteArgumentType(argumentsType, Location);
            _annotatedBytecodeWriter.WriteStackDelta(argumentStackSize, Location);
        }

        public void DeleteObject() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DeleteObject, Location);
        }

        public void Not() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BooleanNot, Location);
        }

        public void Negate() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Negate, Location);
        }

        public void Add() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Add, Location);
        }

        public void Subtract() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Subtract, Location);
        }

        public void Multiply() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Multiply, Location);
        }

        public void MultiplyReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.MultiplyReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Divide() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Divide, Location);
        }

        public void DivideReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.DivideReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Modulus() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Modulus, Location);
        }

        public void ModulusModulus() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ModulusModulus, Location);
        }

        public void ModulusReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ModulusReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void ModulusModulusReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ModulusModulusReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Power() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Power, Location);
        }

        public void Append(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Append, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Increment(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Increment, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Decrement(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Decrement, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Remove(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Remove, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Combine(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Combine, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void Mask(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Mask, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void BitShiftLeft() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitShiftLeft, Location);
        }

        public void BitShiftLeftReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitShiftLeftReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void BitShiftRight() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitShiftRight, Location);
        }

        public void BitShiftRightReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitShiftRightReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void BinaryNot() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitNot, Location);
        }

        public void BinaryAnd() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitAnd, Location);
        }

        public void BinaryXor() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitXor, Location);
        }

        public void BinaryXorReference(DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitXorReference, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void BinaryOr() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.BitOr, Location);
        }

        public void Equal() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareEquals, Location);
        }

        public void NotEqual() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareNotEquals, Location);
        }

        public void Equivalent() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareEquivalent, Location);
        }

        public void NotEquivalent() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareNotEquivalent, Location);
        }

        public void GreaterThan() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareGreaterThan, Location);
        }

        public void GreaterThanOrEqual() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareGreaterThanOrEqual, Location);
        }

        public void LessThan() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareLessThan, Location);
        }

        public void LessThanOrEqual() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.CompareLessThanOrEqual, Location);
        }

        public void Sin() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Sin, Location);
        }

        public void Cos() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Cos, Location);
        }

        public void Tan() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Tan, Location);
        }

        public void ArcSin() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ArcSin, Location);
        }

        public void ArcCos() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ArcCos, Location);
        }

        public void ArcTan() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ArcTan, Location);
        }

        public void ArcTan2() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.ArcTan2, Location);
        }

        public void Sqrt() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Sqrt, Location);
        }

        public void Log() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Log, Location);
        }

        public void LogE() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.LogE, Location);
        }

        public void Abs() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Abs, Location);
        }

        public void PushFloat(float value) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushFloat, Location);
            _annotatedBytecodeWriter.WriteFloat(value, Location);
        }

        public void PushString(string value) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushString, Location);
            _annotatedBytecodeWriter.WriteString(value, Location);
        }

        public void PushResource(string value) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushResource, Location);
            _annotatedBytecodeWriter.WriteResource(value, Location);
        }

        public void PushType(int typeId, DreamPath? type) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushType, Location);
            _annotatedBytecodeWriter.WriteTypeId(typeId, type, Location);
        }

        public void PushProc(int procId, DreamPath? proc) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushProc, Location);
            _annotatedBytecodeWriter.WriteProcId(procId, proc, Location);
        }

        public void PushNull() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushNull, Location);
        }

        public void PushGlobalVars() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PushGlobalVars, Location);
        }

        public void FormatString(string value) {
            int formatCount = 0;
            for (int i = 0; i < value.Length; i++) {
                if (StringFormatEncoder.Decode(value[i], out var formatType)) {
                    if (StringFormatEncoder.IsInterpolation(formatType.Value))
                        formatCount++;
                }
            }

            _annotatedBytecodeWriter.ResizeStack(-(formatCount - 1)); //Shrinks by the amount of formats in the string, grows 1
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.FormatString, Location);
            _annotatedBytecodeWriter.WriteString(value, Location);
            _annotatedBytecodeWriter.WriteFormatCount(formatCount, Location);
        }

        public void IsInList() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.IsInList, Location);
        }

        public void IsInRange() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.IsInRange, Location);
        }

        public void IsSaved() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.IsSaved, Location);
        }

        public void IsType() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.IsType, Location);
        }

        public void IsNull() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.IsNull, Location);
        }

        public void Length() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Length, Location);
        }

        public void GetStep() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.GetStep, Location);
        }

        public void GetDir() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.GetDir, Location);
        }

        public void LocateCoordinates() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.LocateCoord, Location);
        }

        public void Gradient(DMCallArgumentsType argumentsType, int argumentStackSize) {
            _annotatedBytecodeWriter.ResizeStack(-(argumentStackSize - 1)); // Pops arguments, pushes gradient result
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Gradient, Location);
            _annotatedBytecodeWriter.WriteArgumentType(argumentsType, Location);
            _annotatedBytecodeWriter.WriteStackDelta(argumentStackSize, Location);
        }

        public void PickWeighted(int count) {
            _annotatedBytecodeWriter.ResizeStack(-(count - 1));
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PickWeighted, Location);
            _annotatedBytecodeWriter.WritePickCount(count, Location);
        }

        public void PickUnweighted(int count) {
            _annotatedBytecodeWriter.ResizeStack(-(count - 1));
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.PickUnweighted, Location);
            _annotatedBytecodeWriter.WritePickCount(count, Location);
        }

        public void Prob() {
            //Pops 1, pushes 1
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Prob, Location);
        }

        public void MassConcatenation(int count) {
            _annotatedBytecodeWriter.ResizeStack(-(count - 1));
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.MassConcatenation, Location);
            _annotatedBytecodeWriter.WriteConcatCount(count, Location);
        }

        public void Locate() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Locate, Location);
        }

        public void StartTry(string label, DMReference reference) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.Try, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
            _annotatedBytecodeWriter.WriteReference(reference, Location);
        }

        public void StartTryNoValue(string label) {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.TryNoValue, Location);
            _annotatedBytecodeWriter.WriteLabel(label, Location);
        }

        public void EndTry() {
            _annotatedBytecodeWriter.WriteOpcode(DreamProcOpcode.EndTry, Location);
        }

        public void ResolveLabels() {
            _annotatedBytecodeWriter.ResolveLabels(_pendingLabelReferences, ref _labels);
        }
    }
}
