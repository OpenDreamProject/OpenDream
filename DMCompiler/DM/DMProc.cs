using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DMCompiler.Bytecode;
using DMCompiler.Compiler.DM;
using DMCompiler.DM.Expressions;
using DMCompiler.DM.Optimizer;
using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;

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
            private static int _idCounter = 0;
            public readonly long AnnotatedByteOffset;
            public readonly int Id;
            public readonly string Name;

            public int ReferencedCount = 0;

            public string LabelName => $"{Name}_{Id}_codelabel";


            public CodeLabel(string name, long offset) {
                Id = _idCounter++;
                Name = name;
                AnnotatedByteOffset = offset;
            }
        }

        internal class DMProcScope {
            public readonly Dictionary<string, LocalVariable> LocalVariables = new();
            public readonly Dictionary<string, CodeLabel> LocalCodeLabels = new();
            public readonly DMProcScope? ParentScope;

            public DMProcScope() { }

            public DMProcScope(DMProcScope? parentScope) {
                ParentScope = parentScope;
            }
        }

        private readonly List<string> _localVariableNames = new();
        private readonly List<SourceInfoJson> _sourceInfo = new();
        private Dictionary<string, long> _annotatedBytecodeLabels = new();
        private DMASTProcDefinition? _astDefinition;

        private DMObject _dmObject;
        private int _labelIdCounter;
        private string? _lastSourceFile;
        private int _localVariableIdCounter;
        private Stack<string>? _loopStack = null;
        private Dictionary<string, LocalVariable> _parameters = new();
        private Stack<CodeLabelReference> _pendingLabelReferences = new();
        private Stack<DMProcScope> _scopes = new();
        private Location _writerLocation;
        public AnnotatedByteCodeWriter AnnotatedBytecode = new();
        public ProcAttributes Attributes;
        public Dictionary<string, int> GlobalVariables = new();
        public int Id;
        public sbyte Invisibility;
        public bool IsVerb = false;
        public Location Location;

        public List<string> Parameters = new();
        public List<DMValueType> ParameterTypes = new();
        public string? VerbCategory = string.Empty;
        public string? VerbDesc;

        public string? VerbName;

        public DMProc(int id, DMObject dmObject, DMASTProcDefinition? astDefinition) {
            Id = id;
            _dmObject = dmObject;
            _astDefinition = astDefinition;
            if (_astDefinition?.IsOverride ?? false)
                Attributes |= ProcAttributes.IsOverride; // init procs don't have AST definitions
            Location = astDefinition?.Location ?? Location.Unknown;
            _writerLocation = Location;
            AnnotatedBytecode = new AnnotatedByteCodeWriter();
            _scopes.Push(new DMProcScope());
        }

        public string Name => _astDefinition?.Name ?? "<init>";

        private int AllocLocalVariable(string name) {
            _localVariableNames.Add(name);
            AnnotatedBytecode.WriteLocalVariable(name, _writerLocation);
            return _localVariableIdCounter++;
        }

        private void DeallocLocalVariables(int amount) {
            if (amount > 0) {
                _localVariableNames.RemoveRange(_localVariableNames.Count - amount, amount);
                AnnotatedBytecode.WriteLocalVariableDealloc(amount, _writerLocation);
                _localVariableIdCounter -= amount;
            }
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

        public ProcDefinitionJson GetJsonRepresentation(StringBuilder? stringBuilder) {
            ProcDefinitionJson procDefinition = new ProcDefinitionJson();

            procDefinition.OwningTypeId = _dmObject.Id;
            procDefinition.Name = Name;
            procDefinition.IsVerb = IsVerb;

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

            BytecodeOptimizer optimizer = new();

            var bytecodelist = optimizer.Optimize(AnnotatedBytecode.GetAnnotatedBytecode(),
                $"{_dmObject.Path.PathString}{Name}");

            //procDefinition.MaxStackSize = optimizer.GetMaxStackSize();
            procDefinition.MaxStackSize = AnnotatedBytecode.GetMaxStackSize();
            AnnotatedBytecodeSerializer serializer = new();

            if (bytecodelist.Count > 0)
                procDefinition.Bytecode =
                    serializer.Serialize(AnnotatedBytecode.GetAnnotatedBytecode());

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
                procDefinition.Locals = serializer.GetLocalVariablesJSON();
            }

            procDefinition.SourceInfo = serializer.SourceInfo;

            if (stringBuilder is not null) {
                var pathString = _dmObject.Path.ToString() == "/" ? "<global>" : _dmObject.Path.ToString();
                var attributeString = Attributes.ToString().Replace(", ", " | ");
                if (attributeString != "0") {
                    stringBuilder.AppendLine($"[{attributeString}]");
                }

                stringBuilder.Append($"Proc {pathString}/{(IsVerb ? "verb/" : "")}{Name}(");
                for (int i = 0; i < Parameters.Count; i++) {
                    string argumentName = Parameters[i];
                    DMValueType argumentType = ParameterTypes[i];

                    stringBuilder.Append($"{argumentName}: {argumentType}");
                    if (i < Parameters.Count - 1) {
                        stringBuilder.Append(", ");
                    }
                }

                stringBuilder.AppendLine("):");
                var bytecode = AnnotatedBytecode.GetAnnotatedBytecode();
                AnnotatedBytecodePrinter.Print(bytecode, _sourceInfo, stringBuilder);
                stringBuilder.AppendLine();
            }

            return procDefinition;
        }

        public string GetLocalVarName(int index) {
            return _localVariableNames[index];
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

            CodeLabel label = new CodeLabel(name, AnnotatedBytecode.Position);
            _scopes.Peek().LocalCodeLabels.Add(name, label);
            return label;
        }

        public void AddLabel(string name) {
            if (_annotatedBytecodeLabels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location,
                    $"A label with the name \"{name}\" already exists");
                return;
            }

            AnnotatedBytecode.AddLabel(name);
            _annotatedBytecodeLabels.Add(name, AnnotatedBytecode.Position);
        }

        public bool TryAddLocalVariable(string name, DreamPath? type) {
            if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
                return false;

            int localVarId = AllocLocalVariable(name);
            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalVariable(localVarId, false, type));
        }

        public bool TryAddLocalConstVariable(string name, DreamPath? type, Constant value) {
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
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Error, _writerLocation);
        }

        public void DebugSource(Location location) {
            var sourceInfo = new SourceInfoJson() {
                Line = location.Line ?? -1
            };

            _writerLocation = location;

            var sourceFile = location.SourceFile.Replace('\\', '/');

            // Only write the source file if it has changed
            if (_lastSourceFile != sourceFile) {
                sourceInfo.File = DMObjectTree.AddString(sourceFile);
                DMCompiler.SourceFilesDictionary.TryAdd(sourceFile, sourceInfo.File.Value);
            } else if (_sourceInfo.Count > 0 && sourceInfo.Line == _sourceInfo[^1].Line) {
                // Don't need to write this source info if it's the same source & line as the last
                return;
            }

            _lastSourceFile = sourceFile;
            _sourceInfo.Add(sourceInfo);
        }

        public void PushReferenceValue(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushReferenceValue, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void CreateListEnumerator() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateListEnumerator, _writerLocation);
        }

        public void CreateFilteredListEnumerator(DreamPath filterType) {
            if (!DMObjectTree.TryGetTypeId(filterType, out var filterTypeId)) {
                DMCompiler.ForcedError($"Cannot filter enumeration by type {filterType}");
            }

            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateFilteredListEnumerator, _writerLocation);
            AnnotatedBytecode.WriteFilterID(filterTypeId, filterType, _writerLocation);
        }

        public void CreateTypeEnumerator() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateTypeEnumerator, _writerLocation);
        }

        public void CreateRangeEnumerator() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateRangeEnumerator, _writerLocation);
        }

        public void Enumerate(DMReference reference) {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Enumerate, _writerLocation);
                AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
                AnnotatedBytecode.WriteLabel($"{peek}_end", _writerLocation);
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void EnumerateNoAssign() {
            if (_loopStack?.TryPeek(out var peek) ?? false) {
                AnnotatedBytecode.WriteOpcode(DreamProcOpcode.EnumerateNoAssign, _writerLocation);
                AnnotatedBytecode.WriteLabel($"{peek}_end", _writerLocation);
            } else {
                DMCompiler.ForcedError(Location, "Cannot peek empty loop stack");
            }
        }

        public void DestroyEnumerator() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DestroyEnumerator, _writerLocation);
        }

        public void CreateList(int size) {
            AnnotatedBytecode.ResizeStack(-(size - 1)); //Shrinks by the size of the list, grows by 1
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateList, _writerLocation);
            AnnotatedBytecode.WriteListSize(size, _writerLocation);
        }

        public void CreateAssociativeList(int size) {
            AnnotatedBytecode.ResizeStack(-(size * 2 - 1)); //Shrinks by twice the size of the list, grows by 1
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateAssociativeList, _writerLocation);
            AnnotatedBytecode.WriteListSize(size, _writerLocation);
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
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.SwitchCase, _writerLocation);
            AnnotatedBytecode.WriteLabel(caseLabel, _writerLocation);
        }

        public void SwitchCaseRange(string caseLabel) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.SwitchCaseRange, _writerLocation);
            AnnotatedBytecode.WriteLabel(caseLabel, _writerLocation);
        }

        public void Browse() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Browse, _writerLocation);
        }

        public void BrowseResource() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BrowseResource, _writerLocation);
        }

        public void OutputControl() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.OutputControl, _writerLocation);
        }

        public void Ftp() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Ftp, _writerLocation);
        }

        public void OutputReference(DMReference leftRef) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.OutputReference, _writerLocation);
            AnnotatedBytecode.WriteReference(leftRef, _writerLocation, ResolveReferenceToString(leftRef));
        }

        public void Output() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Output, _writerLocation);
        }

        public void Input(DMReference leftRef, DMReference rightRef) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Input, _writerLocation);
            AnnotatedBytecode.WriteReference(leftRef, _writerLocation, ResolveReferenceToString(leftRef));
            AnnotatedBytecode.WriteReference(rightRef, _writerLocation, ResolveReferenceToString(rightRef));
        }

        public void Spawn(string jumpTo) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Spawn, _writerLocation);
            AnnotatedBytecode.WriteLabel(jumpTo, _writerLocation);
        }

        public void Break(DMASTIdentifier? label = null) {
            if (label is not null) {
                var codeLabel = (AnnotatedBytecode.GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
                                 label.Identifier + "_codelabel"
                    );
                if (!_annotatedBytecodeLabels.ContainsKey(codeLabel)) {
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
                    AnnotatedBytecode.GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
                    label.Identifier + "_codelabel"
                );
                if (!_annotatedBytecodeLabels.ContainsKey(codeLabel)) {
                    DMCompiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
                }

                var labelList = _annotatedBytecodeLabels.Keys.ToList();
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
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Pop, _writerLocation);
        }

        public void PopReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PopReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference), false);
        }

        public void BooleanOr(string endLabel) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BooleanOr, _writerLocation);
            AnnotatedBytecode.WriteLabel(endLabel, _writerLocation);
        }

        public void BooleanAnd(string endLabel) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BooleanAnd, _writerLocation);
            AnnotatedBytecode.WriteLabel(endLabel, _writerLocation);
        }

        public void StartScope() {
            _scopes.Push(new DMProcScope(_scopes.Peek()));
        }

        public void EndScope() {
            DMProcScope destroyedScope = _scopes.Pop();
            DeallocLocalVariables(destroyedScope.LocalVariables.Count);
        }

        public void Jump(string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Jump, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfFalse(string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfFalse, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfTrue(string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfTrue, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        //Jumps to the label and pushes null if the reference is dereferencing null
        public void JumpIfNullDereference(DMReference reference, string label) {
            //Either grows the stack by 0 or 1. Assume 0.
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfNullDereference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference),
                affectStack: false);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfNull(string label) {
            // Conditionally pops one value
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfNull, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfNullNoPop(string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfNullNoPop, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfTrueReference(DMReference reference, string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfTrueReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference),
                affectStack: false);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void JumpIfFalseReference(DMReference reference, string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.JumpIfFalseReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference),
                affectStack: false);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void DereferenceField(string field) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DereferenceField, _writerLocation);
            AnnotatedBytecode.WriteString(field, _writerLocation);
        }

        public void DereferenceIndex() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DereferenceIndex, _writerLocation);
        }

        public void DereferenceCall(string field, DMCallArgumentsType argumentsType, int argumentStackSize) {
            AnnotatedBytecode.ResizeStack(-argumentStackSize); // Pops proc owner and arguments, pushes result
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DereferenceCall, _writerLocation);
            AnnotatedBytecode.WriteString(field, _writerLocation);
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
            AnnotatedBytecode.WriteStackDelta(argumentStackSize, _writerLocation);
        }

        public void Call(DMReference reference, DMCallArgumentsType argumentsType, int argumentStackSize) {
            AnnotatedBytecode.ResizeStack(-(argumentStackSize - 1)); // Pops all arguments, pushes return value
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Call, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
            AnnotatedBytecode.WriteStackDelta(argumentStackSize, _writerLocation);
        }

        public void CallStatement(DMCallArgumentsType argumentsType, int argumentStackSize) {
            //Shrinks the stack by argumentStackSize. Could also shrink it by argumentStackSize+1, but assume not.
            AnnotatedBytecode.ResizeStack(-argumentStackSize);
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CallStatement, _writerLocation);
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
            AnnotatedBytecode.WriteStackDelta(argumentStackSize, _writerLocation);
        }

        public void Prompt(DMValueType types) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Prompt, _writerLocation);
            AnnotatedBytecode.WriteType(types, _writerLocation);
        }

        public void Initial() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Initial, _writerLocation);
        }

        public void Return() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Return, _writerLocation);
        }

        public void Throw() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Throw, _writerLocation);
        }

        public void Assign(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Assign, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void AssignInto(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.AssignInto, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void CreateObject(DMCallArgumentsType argumentsType, int argumentStackSize) {
            AnnotatedBytecode.ResizeStack(-argumentStackSize); // Pops type and arguments, pushes new object
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CreateObject, _writerLocation);
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
            AnnotatedBytecode.WriteStackDelta(argumentStackSize, _writerLocation);
        }

        public void DeleteObject() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DeleteObject, _writerLocation);
        }

        public void Not() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BooleanNot, _writerLocation);
        }

        public void Negate() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Negate, _writerLocation);
        }

        public void Add() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Add, _writerLocation);
        }

        public void Subtract() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Subtract, _writerLocation);
        }

        public void Multiply() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Multiply, _writerLocation);
        }

        public void MultiplyReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.MultiplyReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Divide() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Divide, _writerLocation);
        }

        public void DivideReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.DivideReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Modulus() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Modulus, _writerLocation);
        }

        public void ModulusModulus() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ModulusModulus, _writerLocation);
        }

        public void ModulusReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ModulusReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void ModulusModulusReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ModulusModulusReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Power() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Power, _writerLocation);
        }

        public void Append(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Append, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Increment(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Increment, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Decrement(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Decrement, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Remove(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Remove, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Combine(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Combine, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void Mask(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Mask, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void BitShiftLeft() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitShiftLeft, _writerLocation);
        }

        public void BitShiftLeftReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitShiftLeftReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void BitShiftRight() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitShiftRight, _writerLocation);
        }

        public void BitShiftRightReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitShiftRightReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void BinaryNot() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitNot, _writerLocation);
        }

        public void BinaryAnd() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitAnd, _writerLocation);
        }

        public void BinaryXor() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitXor, _writerLocation);
        }

        public void BinaryXorReference(DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitXorReference, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void BinaryOr() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.BitOr, _writerLocation);
        }

        public void Equal() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareEquals, _writerLocation);
        }

        public void NotEqual() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareNotEquals, _writerLocation);
        }

        public void Equivalent() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareEquivalent, _writerLocation);
        }

        public void NotEquivalent() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareNotEquivalent, _writerLocation);
        }

        public void GreaterThan() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareGreaterThan, _writerLocation);
        }

        public void GreaterThanOrEqual() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareGreaterThanOrEqual, _writerLocation);
        }

        public void LessThan() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareLessThan, _writerLocation);
        }

        public void LessThanOrEqual() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.CompareLessThanOrEqual, _writerLocation);
        }

        public void Sin() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Sin, _writerLocation);
        }

        public void Cos() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Cos, _writerLocation);
        }

        public void Tan() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Tan, _writerLocation);
        }

        public void ArcSin() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ArcSin, _writerLocation);
        }

        public void ArcCos() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ArcCos, _writerLocation);
        }

        public void ArcTan() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ArcTan, _writerLocation);
        }

        public void ArcTan2() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.ArcTan2, _writerLocation);
        }

        public void Sqrt() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Sqrt, _writerLocation);
        }

        public void Log() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Log, _writerLocation);
        }

        public void LogE() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.LogE, _writerLocation);
        }

        public void Abs() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Abs, _writerLocation);
        }

        public void PushFloat(float value) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushFloat, _writerLocation);
            AnnotatedBytecode.WriteFloat(value, _writerLocation);
        }

        public void PushString(string value) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushString, _writerLocation);
            AnnotatedBytecode.WriteString(value, _writerLocation);
        }

        public void PushResource(string value) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushResource, _writerLocation);
            AnnotatedBytecode.WriteResource(value, _writerLocation);
        }

        public void PushType(int typeId, DreamPath? type) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushType, _writerLocation);
            AnnotatedBytecode.WriteTypeId(typeId, type, _writerLocation);
        }

        public void PushProc(int procId, DreamPath? proc) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushProc, _writerLocation);
            AnnotatedBytecode.WriteProcId(procId, proc, _writerLocation);
        }

        public void PushNull() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushNull, _writerLocation);
        }

        public void PushGlobalVars() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PushGlobalVars, _writerLocation);
        }

        public void FormatString(string value) {
            int formatCount = 0;
            for (int i = 0; i < value.Length; i++) {
                if (StringFormatEncoder.Decode(value[i], out var formatType)) {
                    if (StringFormatEncoder.IsInterpolation(formatType.Value))
                        formatCount++;
                }
            }

            AnnotatedBytecode.ResizeStack(-(formatCount - 1)); //Shrinks by the amount of formats in the string, grows 1
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.FormatString, _writerLocation);
            AnnotatedBytecode.WriteString(value, _writerLocation);
            AnnotatedBytecode.WriteFormatCount(formatCount, _writerLocation);
        }

        public void IsInList() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.IsInList, _writerLocation);
        }

        public void IsInRange() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.IsInRange, _writerLocation);
        }

        public void IsSaved() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.IsSaved, _writerLocation);
        }

        public void IsType() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.IsType, _writerLocation);
        }

        public void IsNull() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.IsNull, _writerLocation);
        }

        public void Length() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Length, _writerLocation);
        }

        public void GetStep() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.GetStep, _writerLocation);
        }

        public void GetDir() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.GetDir, _writerLocation);
        }

        public void LocateCoordinates() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.LocateCoord, _writerLocation);
        }

        public void Gradient(DMCallArgumentsType argumentsType, int argumentStackSize) {
            AnnotatedBytecode.ResizeStack(-(argumentStackSize - 1)); // Pops arguments, pushes gradient result
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Gradient, _writerLocation);
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
            AnnotatedBytecode.WriteStackDelta(argumentStackSize, _writerLocation);
        }

        public void PickWeighted(int count) {
            AnnotatedBytecode.ResizeStack(-(count - 1));
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PickWeighted, _writerLocation);
            AnnotatedBytecode.WritePickCount(count, _writerLocation);
        }

        public void PickUnweighted(int count) {
            AnnotatedBytecode.ResizeStack(-(count - 1));
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.PickUnweighted, _writerLocation);
            AnnotatedBytecode.WritePickCount(count, _writerLocation);
        }

        public void Prob() {
            //Pops 1, pushes 1
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Prob, _writerLocation);
        }

        public void MassConcatenation(int count) {
            AnnotatedBytecode.ResizeStack(-(count - 1));
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.MassConcatenation, _writerLocation);
            AnnotatedBytecode.WriteConcatCount(count, _writerLocation);
        }

        public void Locate() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Locate, _writerLocation);
        }

        public void StartTry(string label, DMReference reference) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.Try, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
            AnnotatedBytecode.WriteReference(reference, _writerLocation, ResolveReferenceToString(reference));
        }

        public void StartTryNoValue(string label) {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.TryNoValue, _writerLocation);
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        public void EndTry() {
            AnnotatedBytecode.WriteOpcode(DreamProcOpcode.EndTry, _writerLocation);
        }

        public void ResolveLabels() {
            AnnotatedBytecode.ResolveLabels(_pendingLabelReferences, ref _annotatedBytecodeLabels);
        }

        public string GetProcFQN() {
            return $"{_dmObject.Path}/{Name}";
        }

        public string ResolveReferenceToString(DMReference reference) {
            switch (reference.RefType) {
                case DMReference.Type.Src:
                    return "src";
                case DMReference.Type.Self:
                    return $"{GetProcFQN()}";
                case DMReference.Type.Usr:
                    return "usr";
                case DMReference.Type.Args:
                    return "args";
                case DMReference.Type.SuperProc:
                    return "super";
                case DMReference.Type.ListIndex:
                    return $"list[]";
                case DMReference.Type.Argument:
                    return Parameters[int.Max(reference.Index, 0)];
                case DMReference.Type.Local:
                    return _localVariableNames[reference.Index];
                case DMReference.Type.Global:
                    return reference.Name;
                case DMReference.Type.GlobalProc:
                    // Need to find the KEY in the global proc dictionary with the VALUE of the reference index
                    var procName = DMObjectTree.GlobalProcs.FirstOrDefault(x => x.Value == reference.Index).Key;
                    if (DMObjectTree.TryGetGlobalProc(procName, out var proc)) {
                        var path = proc._dmObject.Path.PathString == "/" ? "" : proc._dmObject.Path.PathString;
                        return $"{path}{proc.Name}";
                    } else {
                        return $"proc#{reference.Index}";
                    }
                case DMReference.Type.Field:
                    return reference.Name;
                case DMReference.Type.SrcField:
                    return reference.Name;
                case DMReference.Type.SrcProc:
                    return reference.Name;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


    }
}
