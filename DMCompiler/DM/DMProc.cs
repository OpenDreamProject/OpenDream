using DMCompiler.Bytecode;
using DMCompiler.Compiler.DM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DMCompiler.Bytecode;
using DMCompiler.DM.Expressions;
using DMCompiler.DM.Optimizer;
using DMCompiler.Compiler;
using DMCompiler.DM.Visitors;
using DMCompiler.Json;

namespace DMCompiler.DM {
    internal sealed class DMProc {
        public class LocalVariable(string name, int id, bool isParameter, DreamPath? type) {
            public readonly string Name = name;
            public readonly int Id = id;
            public bool IsParameter = isParameter;
            public DreamPath? Type = type;
        }

        public sealed class LocalConstVariable(string name, int id, DreamPath? type, Expressions.Constant value)
                : LocalVariable(name, id, false, type) {
            public readonly Expressions.Constant Value = value;
        }

        internal struct CodeLabelReference(string identifier, string placeholder, Location location, DMProcScope scope) {
            public readonly string Identifier = identifier;
            public readonly string Placeholder = placeholder;
            public readonly Location Location = location;
            public readonly DMProcScope Scope = scope;
        }

        public class CodeLabel {
            private static int _idCounter = 0;
            public readonly long AnnotatedByteOffset;
            public readonly int Id;
            public readonly string Name;

            public int ReferencedCount;

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


        public List<string> Parameters = new();
        public List<DMValueType> ParameterTypes = new();
        public Location Location;
        public ProcAttributes Attributes;
        public bool IsVerb = false;
        public int Id;
        public Dictionary<string, int> GlobalVariables = new();

        public string? VerbName;
        public string? VerbCategory = string.Empty;
        public string? VerbDesc;
        public sbyte Invisibility;

        private DMObject _dmObject;
        private DMASTProcDefinition? _astDefinition;
        private Stack<CodeLabelReference> _pendingLabelReferences = new();
        private Stack<string>? _loopStack = null;
        private Stack<DMProcScope> _scopes = new();
        private Dictionary<string, LocalVariable> _parameters = new();
        private int _labelIdCounter;

        private List<string> _localVariableNames = new();
        private int _localVariableIdCounter;

        private readonly List<SourceInfoJson> _sourceInfo = new();
        private string? _lastSourceFile;

        public long Position => AnnotatedBytecode.Position;
        public AnnotatedByteCodeWriter AnnotatedBytecode = new();

        private Location _writerLocation;


        public DMProc(int id, DMObject dmObject, DMASTProcDefinition? astDefinition) {
            Id = id;
            _dmObject = dmObject;
            _astDefinition = astDefinition;
            if (_astDefinition?.IsOverride ?? false) Attributes |= ProcAttributes.IsOverride; // init procs don't have AST definitions
            Location = astDefinition?.Location ?? Location.Unknown;
            _scopes.Push(new DMProcScope());
        }

        public string Name => _astDefinition?.Name ?? "<init>";

        private int AllocLocalVariable(string name) {
            _localVariableNames.Add(name);
            WriteLocalVariable(name);
            return _localVariableIdCounter++;
        }

        private void DeallocLocalVariables(int amount) {
            if (amount > 0) {
                _localVariableNames.RemoveRange(_localVariableNames.Count - amount, amount);
                WriteLocalVariableDealloc(amount);
                _localVariableIdCounter -= amount;
            }
        }


        public DreamPath GetPath() {
            return _dmObject.Path;
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
                $"{_dmObject.Path.PathString}{Name}", out int stackDepth);

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

            return procDefinition;
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
                _parameters.Add(name, new LocalVariable(name, _parameters.Count, true, type));
            }
        }

        public string MakePlaceholderLabel() => $"PLACEHOLDER_{_pendingLabelReferences.Count}_LABEL";

        public CodeLabel? TryAddCodeLabel(string name) {
            if (_scopes.Peek().LocalCodeLabels.ContainsKey(name)) {
                DMCompiler.Emit(WarningCode.DuplicateVariable, Location, $"A label with the name \"{name}\" already exists");
                return null;
            }

            CodeLabel label = new CodeLabel(name, Position);
            _scopes.Peek().LocalCodeLabels.Add(name, label);
            return label;
        }

        public bool TryAddLocalVariable(string name, DreamPath? type) {
            if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
                return false;

            int localVarId = AllocLocalVariable(name);
            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalVariable(name, localVarId, false, type));
        }

        public bool TryAddLocalConstVariable(string name, DreamPath? type, Expressions.Constant value) {
            if (_parameters.ContainsKey(name)) //Parameters and local vars cannot share a name
                return false;

            int localVarId = AllocLocalVariable(name);
            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalConstVariable(name, localVarId, type, value));
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
                Line = location.Line ?? -1
            };

            _writerLocation = location;

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
            WriteOpcode(DreamProcOpcode.PushReferenceValue);
            WriteReference(reference);
        }

        public void CreateListEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        }

        public void CreateFilteredListEnumerator(DreamPath filterType) {
            if (!DMObjectTree.TryGetTypeId(filterType, out var filterTypeId)) {
                DMCompiler.ForcedError($"Cannot filter enumeration by type {filterType}");
            }

            WriteOpcode(DreamProcOpcode.CreateFilteredListEnumerator);
            WriteFilterID(filterTypeId, filterType);
        }

        public void CreateTypeEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateTypeEnumerator);
        }

        public void CreateRangeEnumerator() {
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
            ResizeStack(-(size - 1)); //Shrinks by the size of the list, grows by 1
            WriteOpcode(DreamProcOpcode.CreateList);
            WriteListSize(size);
        }

        public void CreateAssociativeList(int size) {
            ResizeStack(-(size * 2 - 1)); //Shrinks by twice the size of the list, grows by 1
            WriteOpcode(DreamProcOpcode.CreateAssociativeList);
            WriteListSize(size);
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
            WriteOpcode(DreamProcOpcode.SwitchCase);
            WriteLabel(caseLabel);
        }

        public void SwitchCaseRange(string caseLabel) {
            WriteOpcode(DreamProcOpcode.SwitchCaseRange);
            WriteLabel(caseLabel);
        }

        public void Browse() {
            WriteOpcode(DreamProcOpcode.Browse);
        }

        public void BrowseResource() {
            WriteOpcode(DreamProcOpcode.BrowseResource);
        }

        public void OutputControl() {
            WriteOpcode(DreamProcOpcode.OutputControl);
        }

        public void Ftp() {
            WriteOpcode(DreamProcOpcode.Ftp);
        }

        public void OutputReference(DMReference leftRef) {
            WriteOpcode(DreamProcOpcode.OutputReference);
            WriteReference(leftRef);
        }

        public void Output() {
            WriteOpcode(DreamProcOpcode.Output);
        }

        public void Input(DMReference leftRef, DMReference rightRef) {
            WriteOpcode(DreamProcOpcode.Input);
            WriteReference(leftRef);
            WriteReference(rightRef);
        }

        public void Spawn(string jumpTo) {
            WriteOpcode(DreamProcOpcode.Spawn);
            WriteLabel(jumpTo);
        }

        public void Break(DMASTIdentifier? label = null) {
            if (label is not null) {
                var codeLabel = (GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ?? label.Identifier + "_codelabel");
                if (!LabelExists(codeLabel)) {
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
                    GetCodeLabel(label.Identifier, _scopes.Peek())?.LabelName ??
                    label.Identifier + "_codelabel"
                );
                if (!LabelExists(codeLabel)) {
                    DMCompiler.Emit(WarningCode.ItemDoesntExist, label.Location, $"Unknown label {label.Identifier}");
                }

                var labelList = GetLabels().Keys.ToList();
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
            WriteOpcode(DreamProcOpcode.Pop);
        }

        public void PopReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.PopReference);
            WriteReference(reference, false);
        }

        public void BooleanOr(string endLabel) {
            WriteOpcode(DreamProcOpcode.BooleanOr);
            WriteLabel(endLabel);
        }

        public void BooleanAnd(string endLabel) {
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
            WriteOpcode(DreamProcOpcode.JumpIfFalse);
            WriteLabel(label);
        }

        public void JumpIfTrue(string label) {
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
            WriteOpcode(DreamProcOpcode.DereferenceIndex);
        }

        public void DereferenceCall(string field, DMCallArgumentsType argumentsType, int argumentStackSize) {
            ResizeStack(-argumentStackSize); // Pops proc owner and arguments, pushes result
            WriteOpcode(DreamProcOpcode.DereferenceCall);
            WriteString(field);
            WriteArgumentType(argumentsType);
            WriteStackDelta(argumentStackSize);
        }

        public void Call(DMReference reference, DMCallArgumentsType argumentsType, int argumentStackSize) {
            ResizeStack(-(argumentStackSize - 1)); // Pops all arguments, pushes return value
            WriteOpcode(DreamProcOpcode.Call);
            WriteReference(reference);
            WriteArgumentType(argumentsType);
            WriteStackDelta(argumentStackSize);
        }

        public void CallStatement(DMCallArgumentsType argumentsType, int argumentStackSize) {
            //Shrinks the stack by argumentStackSize. Could also shrink it by argumentStackSize+1, but assume not.
            ResizeStack(-argumentStackSize);
            WriteOpcode(DreamProcOpcode.CallStatement);
            WriteArgumentType(argumentsType);
            WriteStackDelta(argumentStackSize);
        }

        public void Prompt(DMValueType types) {
            WriteOpcode(DreamProcOpcode.Prompt);
            WriteType(types);
        }

        public void Initial() {
            WriteOpcode(DreamProcOpcode.Initial);
        }

        public void Return() {
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
            ResizeStack(-argumentStackSize); // Pops type and arguments, pushes new object
            WriteOpcode(DreamProcOpcode.CreateObject);
            WriteArgumentType(argumentsType);
            WriteStackDelta(argumentStackSize);
        }

        public void DeleteObject() {
            WriteOpcode(DreamProcOpcode.DeleteObject);
        }

        public void Not() {
            WriteOpcode(DreamProcOpcode.BooleanNot);
        }

        public void Negate() {
            WriteOpcode(DreamProcOpcode.Negate);
        }

        public void Add() {
            WriteOpcode(DreamProcOpcode.Add);
        }

        public void Subtract() {
            WriteOpcode(DreamProcOpcode.Subtract);
        }

        public void Multiply() {
            WriteOpcode(DreamProcOpcode.Multiply);
        }

        public void MultiplyReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.MultiplyReference);
            WriteReference(reference);
        }

        public void Divide() {
            WriteOpcode(DreamProcOpcode.Divide);
        }

        public void DivideReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.DivideReference);
            WriteReference(reference);
        }

        public void Modulus() {
            WriteOpcode(DreamProcOpcode.Modulus);
        }

        public void ModulusModulus() {
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
            WriteOpcode(DreamProcOpcode.Power);
        }

        public void Append(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Append);
            WriteReference(reference);
        }

        public void Increment(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Increment);
            WriteReference(reference);
        }

        public void Decrement(DMReference reference) {
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
            WriteOpcode(DreamProcOpcode.BitShiftLeft);
        }

        public void BitShiftLeftReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.BitShiftLeftReference);
            WriteReference(reference);
        }

        public void BitShiftRight() {
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
            WriteOpcode(DreamProcOpcode.BitAnd);
        }

        public void BinaryXor() {
            WriteOpcode(DreamProcOpcode.BitXor);
        }

        public void BinaryXorReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.BitXorReference);
            WriteReference(reference);
        }

        public void BinaryOr() {
            WriteOpcode(DreamProcOpcode.BitOr);
        }

        public void Equal() {
            WriteOpcode(DreamProcOpcode.CompareEquals);
        }

        public void NotEqual() {
            WriteOpcode(DreamProcOpcode.CompareNotEquals);
        }

        public void Equivalent() {
            WriteOpcode(DreamProcOpcode.CompareEquivalent);
        }

        public void NotEquivalent() {
            WriteOpcode(DreamProcOpcode.CompareNotEquivalent);
        }

        public void GreaterThan() {
            WriteOpcode(DreamProcOpcode.CompareGreaterThan);
        }

        public void GreaterThanOrEqual() {
            WriteOpcode(DreamProcOpcode.CompareGreaterThanOrEqual);
        }

        public void LessThan() {
            WriteOpcode(DreamProcOpcode.CompareLessThan);
        }

        public void LessThanOrEqual() {
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
            WriteOpcode(DreamProcOpcode.ArcTan2);
        }

        public void Sqrt() {
            WriteOpcode(DreamProcOpcode.Sqrt);
        }

        public void Log() {
            WriteOpcode(DreamProcOpcode.Log);
        }

        public void LogE() {
            WriteOpcode(DreamProcOpcode.LogE);
        }

        public void Abs() {
            WriteOpcode(DreamProcOpcode.Abs);
        }

        public void PushFloat(float value) {
            WriteOpcode(DreamProcOpcode.PushFloat);
            WriteFloat(value);
        }

        public void PushString(string value) {
            WriteOpcode(DreamProcOpcode.PushString);
            WriteString(value);
        }

        public void PushResource(string value) {
            WriteOpcode(DreamProcOpcode.PushResource);
            WriteResource(value);
        }

        public void PushType(int typeId, DreamPath? type) {
            WriteOpcode(DreamProcOpcode.PushType);
            WriteTypeId(typeId, type);
        }

        public void PushProc(int procId, DreamPath? proc) {
            WriteOpcode(DreamProcOpcode.PushProc);
            WriteProcId(procId, proc);
        }

        public void PushNull() {
            WriteOpcode(DreamProcOpcode.PushNull);
        }

        public void PushGlobalVars() {
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

            ResizeStack(-(formatCount - 1)); //Shrinks by the amount of formats in the string, grows 1
            WriteOpcode(DreamProcOpcode.FormatString);
            WriteString(value);
            WriteFormatCount(formatCount);
        }

        public void IsInList() {
            WriteOpcode(DreamProcOpcode.IsInList);
        }

        public void IsInRange() {
            WriteOpcode(DreamProcOpcode.IsInRange);
        }

        public void IsSaved() {
            WriteOpcode(DreamProcOpcode.IsSaved);
        }

        public void IsType() {
            WriteOpcode(DreamProcOpcode.IsType);
        }

        public void IsNull() {
            WriteOpcode(DreamProcOpcode.IsNull);
        }

        public void Length() {
            WriteOpcode(DreamProcOpcode.Length);
        }

        public void GetStep() {
            WriteOpcode(DreamProcOpcode.GetStep);
        }

        public void GetDir() {
            WriteOpcode(DreamProcOpcode.GetDir);
        }

        public void LocateCoordinates() {
            WriteOpcode(DreamProcOpcode.LocateCoord);
        }

        public void Gradient(DMCallArgumentsType argumentsType, int argumentStackSize) {
            ResizeStack(-(argumentStackSize - 1)); // Pops arguments, pushes gradient result
            WriteOpcode(DreamProcOpcode.Gradient);
            WriteArgumentType(argumentsType);
            WriteStackDelta(argumentStackSize);
        }

        public void PickWeighted(int count) {
            ResizeStack(-(count - 1));
            WriteOpcode(DreamProcOpcode.PickWeighted);
            WritePickCount(count);
        }

        public void PickUnweighted(int count) {
            ResizeStack(-(count - 1));
            WriteOpcode(DreamProcOpcode.PickUnweighted);
            WritePickCount(count);
        }

        public void Prob() {
            //Pops 1, pushes 1
            WriteOpcode(DreamProcOpcode.Prob);
        }

        public void MassConcatenation(int count) {
            ResizeStack(-(count - 1));
            WriteOpcode(DreamProcOpcode.MassConcatenation);
            WriteConcatCount(count);
        }

        public void Locate() {
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

        // Annotated bytecode wrapper procedures
        private void WriteOpcode(DreamProcOpcode opcode) {
            AnnotatedBytecode.WriteOpcode(opcode, _writerLocation);
        }

        private void WriteReference(DMReference reference, bool affectStack = true) {
            AnnotatedBytecode.WriteReference(reference, _writerLocation, affectStack);
        }

        private void WriteArgumentType(DMCallArgumentsType argumentsType) {
            AnnotatedBytecode.WriteArgumentType(argumentsType, _writerLocation);
        }

        private void WriteLabel(string label) {
            AnnotatedBytecode.WriteLabel(label, _writerLocation);
        }

        private void WriteString(string value) {
            AnnotatedBytecode.WriteString(value, _writerLocation);
        }

        private void WriteResource(string value) {
            AnnotatedBytecode.WriteResource(value, _writerLocation);
        }

        private void WriteTypeId(int typeId, DreamPath? type) {
            AnnotatedBytecode.WriteTypeId(typeId, type, _writerLocation);
        }

        private void WriteProcId(int procId, DreamPath? proc) {
            AnnotatedBytecode.WriteProcId(procId, proc, _writerLocation);
        }

        private void WriteFloat(float value) {
            AnnotatedBytecode.WriteFloat(value, _writerLocation);
        }

        private void WriteListSize(int size) {
            AnnotatedBytecode.WriteListSize(size, _writerLocation);
        }

        private void WriteFormatCount(int count) {
            AnnotatedBytecode.WriteFormatCount(count, _writerLocation);
        }

        private void WritePickCount(int count) {
            AnnotatedBytecode.WritePickCount(count, _writerLocation);
        }

        private void WriteConcatCount(int count) {
            AnnotatedBytecode.WriteConcatCount(count, _writerLocation);
        }

        private void WriteFilterID(int filterId, DreamPath filter) {
            AnnotatedBytecode.WriteFilterID(filterId, filter, _writerLocation);
        }

        private void WriteStackDelta(int delta) {
            AnnotatedBytecode.WriteStackDelta(delta, _writerLocation);
        }

        private void WriteLocalVariable(string name) {
            AnnotatedBytecode.WriteLocalVariable(name, _writerLocation);
        }

        private void WriteLocalVariableDealloc(int amount) {
            AnnotatedBytecode.WriteLocalVariableDealloc(amount, _writerLocation);
        }

        private void ResizeStack(int delta) {
            AnnotatedBytecode.ResizeStack(delta);
        }

        private CodeLabel? GetCodeLabel(string identifier, DMProcScope scope) {
            return AnnotatedBytecode.GetCodeLabel(identifier, scope);
        }

        private void WriteType(DMValueType type) {
            AnnotatedBytecode.WriteType(type, _writerLocation);
        }

        public void AddLabel(string name) {
            AnnotatedBytecode.AddLabel(name);
        }

        private bool LabelExists(string name) {
            return AnnotatedBytecode.LabelExists(name);
        }

        private Dictionary<string, long> GetLabels() {
            return AnnotatedBytecode.GetLabels();
        }

        public void ResolveLabels() {
            AnnotatedBytecode.ResolveCodeLabelReferences(_pendingLabelReferences);
        }

        private static string _lastDumpedFile = "";
        public void Dump(StreamWriter bytecodeDumpWriter) {
            var pathString = _dmObject.Path.ToString() == "/" ? "<global>" : _dmObject.Path.ToString();
            var attributeString = Attributes.ToString().Replace(", ", " | ");
            if (!string.IsNullOrEmpty(_lastSourceFile) && _lastSourceFile != _lastDumpedFile) {
                _lastDumpedFile = _lastSourceFile;
                bytecodeDumpWriter.WriteLine();
                bytecodeDumpWriter.WriteLine();
                bytecodeDumpWriter.Write($"In file {_lastSourceFile} at line {_sourceInfo.FirstOrDefault()?.Line ?? -1}:\n");
            }
            if (attributeString != "0") {
                bytecodeDumpWriter.Write($"[{attributeString}] ");
            }
            bytecodeDumpWriter.Write($"Proc {pathString}/{(IsVerb ? "verb/" : "")}{Name}(");
            for (int i = 0; i < Parameters.Count; i++) {
                string argumentName = Parameters[i];
                DMValueType argumentType = ParameterTypes[i];

                bytecodeDumpWriter.Write($"{argumentName}: {argumentType}");
                if (i < Parameters.Count - 1) {
                    bytecodeDumpWriter.Write(", ");
                }
            }

            bytecodeDumpWriter.Write("):");
            var bytecode = AnnotatedBytecode.GetAnnotatedBytecode();
            if (bytecode.Count > 0) {
                bytecodeDumpWriter.WriteLine();
                AnnotatedBytecodePrinter.Print(bytecode, _sourceInfo, bytecodeDumpWriter, this);
                bytecodeDumpWriter.WriteLine();
            } else {
                bytecodeDumpWriter.Write(" <empty>\n");
            }
        }
    }
}
