using DMCompiler.DM.Visitors;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using OpenDreamShared.Compiler;

namespace DMCompiler.DM {
    class DMProc {
        public class LocalVariable {
            public int Id;
            public DreamPath? Type;

            public LocalVariable(int id, DreamPath? type) {
                Id = id;
                Type = type;
            }
        }

        public class LocalConstVariable : LocalVariable {
            public Expressions.Constant Value;

            public LocalConstVariable(int id, DreamPath? type, Expressions.Constant value) : base(id, type) {
                Value = value;
            }
        }

        private class DMProcScope {
            public Dictionary<string, LocalVariable> LocalVariables = new();
            public DMProcScope ParentScope;

            public DMProcScope() { }

            public DMProcScope(DMProcScope parentScope) {
                ParentScope = parentScope;
            }
        }

        public MemoryStream Bytecode = new MemoryStream();
        public List<string> Parameters = new();
        public List<DMValueType> ParameterTypes = new();
        public bool Unimplemented { get; set; } = false;
        public Location Location = Location.Unknown;
        public string Name { get => _astDefinition?.Name; }
        public bool IsOverride = false;
        public Dictionary<string, int> GlobalVariables = new();

        private DMASTProcDefinition _astDefinition = null;
        private BinaryWriter _bytecodeWriter = null;
        private Dictionary<string, long> _labels = new();
        private List<(long Position, string LabelName)> _unresolvedLabels = new();
        private Stack<string> _loopStack = new();
        private Stack<DMProcScope> _scopes = new();
        private int _localVariableIdCounter = 0;
        private bool _waitFor = true;
        private int _labelIdCounter = 0;
        private int _maxStackSize = 0;
        private int _currentStackSize = 0;

        public DMProc([CanBeNull] DMASTProcDefinition astDefinition) {
            _astDefinition = astDefinition;
            IsOverride = _astDefinition?.IsOverride ?? false; // init procs don't have AST definitions
            Location = astDefinition?.Location ?? Location.Unknown;
            _bytecodeWriter = new BinaryWriter(Bytecode);
            _scopes.Push(new DMProcScope());
        }

        public void Compile(DMObject dmObject) {
            foreach (DMASTDefinitionParameter parameter in _astDefinition.Parameters) {
                AddParameter(parameter.Name, parameter.Type);
            }

            new DMProcBuilder(dmObject, this).ProcessProcDefinition(_astDefinition);
        }

        public ProcDefinitionJson GetJsonRepresentation() {
            ProcDefinitionJson procDefinition = new ProcDefinitionJson();
            if(!_waitFor) procDefinition.WaitFor = _waitFor; // Procs set this to true by default, so only serialize if false
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

            return procDefinition;
        }

        public void WaitFor(bool waitFor) {
            _waitFor = waitFor;
        }

        public DMVariable CreateGlobalVariable(DreamPath? type, string name, bool isConst)
        {
            int id = DMObjectTree.CreateGlobal(out DMVariable global, type, name, isConst);

            GlobalVariables[name] = id;
            return global;
        }

        public int? GetGlobalVariableId(string name)
        {
            if (GlobalVariables.TryGetValue(name, out int id))
            {
                return id;
            }
            return null;
        }

        public DMVariable GetGlobalVariable(string name)
        {
            int? id = GetGlobalVariableId(name);

            return (id == null) ? null : DMObjectTree.Globals[id.Value];
        }

        public void AddParameter(string name, DMValueType type) {
            Parameters.Add(name);
            ParameterTypes.Add(type);
        }

        public void ResolveLabels() {
            foreach ((long Position, string LabelName) unresolvedLabel in _unresolvedLabels) {
                if (_labels.TryGetValue(unresolvedLabel.LabelName, out long labelPosition)) {
                    _bytecodeWriter.Seek((int)unresolvedLabel.Position, SeekOrigin.Begin);
                    WriteInt((int)labelPosition);
                } else {
                    DMCompiler.Error(new CompilerError(Location, "Invalid label \"" + unresolvedLabel.LabelName + "\""));
                }
            }

            _unresolvedLabels.Clear();
            _bytecodeWriter.Seek(0, SeekOrigin.End);
        }

        public void AddLabel(string name) {
            if (_labels.ContainsKey(name)) {
                DMCompiler.Error(new CompilerError(Location, $"A label with the name \"{name}\" already exists"));
                return;
            }

            _labels.Add(name, Bytecode.Position);
        }

        public bool TryAddLocalVariable(string name, DreamPath? type) {
            int localVarId = _localVariableIdCounter++;

            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalVariable(localVarId, type));
        }

        public bool TryAddLocalConstVariable(string name, DreamPath? type, Expressions.Constant value) {
            int localVarId = _localVariableIdCounter++;

            return _scopes.Peek().LocalVariables.TryAdd(name, new LocalConstVariable(localVarId, type, value));
        }

        public LocalVariable GetLocalVariable(string name) {
            DMProcScope scope = _scopes.Peek();

            while (scope != null) {
                if (scope.LocalVariables.TryGetValue(name, out LocalVariable localVariable)) return localVariable;

                scope = scope.ParentScope;
            }

            return null;
        }

        public DMReference GetLocalVariableReference(string name) {
            return DMReference.CreateLocal(GetLocalVariable(name).Id);
        }

        public void Error() {
            WriteOpcode(DreamProcOpcode.Error);
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

        public void CreateRangeEnumerator() {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.CreateRangeEnumerator);
        }

        public void Enumerate(DMReference output) {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.Enumerate);
            WriteReference(output);
        }

        public void DestroyEnumerator() {
            WriteOpcode(DreamProcOpcode.DestroyEnumerator);
        }

        public void CreateList() {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.CreateList);
        }

        public void ListAppend() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.ListAppend);
        }

        public void ListAppendAssociated() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.ListAppendAssociated);
        }

        public string NewLabelName() {
            return "label" + _labelIdCounter++;
        }

        public void LoopStart(string loopLabel) {
            _loopStack.Push(loopLabel);

            AddLabel(loopLabel + "_start");
            StartScope();
        }

        public void LoopContinue(string loopLabel) {
            AddLabel(loopLabel + "_continue");
        }

        public void LoopJumpToStart(string loopLabel) {
            Jump(loopLabel + "_start");
        }

        public void LoopEnd() {
            AddLabel(_loopStack.Pop() + "_end");
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

        public void Spawn(string jumpTo) {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Spawn);
            WriteLabel(jumpTo);
        }

        public void Break(DMASTIdentifier label = null) {
            if (label is not null)
            {
                Jump(label.Identifier + "_end");
            }
            else
            {
                Jump(_loopStack.Peek() + "_end");
            }
        }

        public void BreakIfFalse() {
            JumpIfFalse(_loopStack.Peek() + "_end");
        }

        public void Continue(DMASTIdentifier label = null) {
            // TODO: Clean up this godawful label handling
            if (label is not null)
            {
                var codeLabel = label.Identifier + "_codelabel";
                if (!_labels.ContainsKey(codeLabel))
                {
                    DMCompiler.Error(new CompilerError(Location, $"Unknown label {label.Identifier}"));
                }
                var labelList = _labels.Keys.ToList();
                var continueLabel = string.Empty;
                for (var i = labelList.IndexOf(codeLabel) + 1; i < labelList.Count; i++)
                {
                    if(labelList[i].EndsWith("_start"))
                    {
                        continueLabel = labelList[i].Replace("_start", "_continue");
                        break;
                    }
                }
                Jump(continueLabel);
            }
            else
            {
                Jump(_loopStack.Peek() + "_continue");
            }
        }

        public void ContinueIfFalse() {
            JumpIfFalse(_loopStack.Peek() + "_continue");
        }

        public void Goto(string label) {
            Jump(label + "_codelabel");
        }

        public void Pop()
        {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Pop);
        }

        public void PushProcArguments() {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushProcArguments);
        }

        public void PushArgumentList() {
            WriteOpcode(DreamProcOpcode.PushArgumentList);
        }

        public void PushArguments(int argumentCount, DreamProcOpcodeParameterType[] parameterTypes = null, string[] parameterNames = null) {
            ShrinkStack(argumentCount - 1); //Pops argumentCount, pushes 1
            WriteOpcode(DreamProcOpcode.PushArguments);
            WriteInt(argumentCount);

            if (argumentCount > 0) {
                if (parameterTypes == null || parameterTypes.Length != argumentCount) {
                    throw new ArgumentException("Length of parameter types does not match the argument count");
                }

                int namedParameterIndex = 0;
                foreach (DreamProcOpcodeParameterType parameterType in parameterTypes) {
                    _bytecodeWriter.Write((byte)parameterType);

                    if (parameterType == DreamProcOpcodeParameterType.Named) {
                        if (parameterNames == null)
                            throw new Exception("parameterNames was null while parameterTypes was:" + parameterTypes);
                        WriteString(parameterNames[namedParameterIndex++]);
                    }
                }
            }
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
            _localVariableIdCounter -= destroyedScope.LocalVariables.Count;
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

        public void Call(DMReference reference) {
            WriteOpcode(DreamProcOpcode.Call);
            WriteReference(reference);
        }

        public void CallStatement() {
            ShrinkStack(1); //Either shrinks the stack by 1 or 2. Assume 1.
            WriteOpcode(DreamProcOpcode.CallStatement);
        }

        public void Prompt(DMValueType types) {
            ShrinkStack(3);
            WriteOpcode(DreamProcOpcode.Prompt);
            WriteInt((int)types);
        }

        public void Initial(string propertyName) {
            WriteOpcode(DreamProcOpcode.Initial);
            WriteString(propertyName);
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

        public void CreateObject() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.CreateObject);
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

        public void ModulusReference(DMReference reference) {
            WriteOpcode(DreamProcOpcode.ModulusReference);
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

        public void BitShiftRight() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.BitShiftRight);
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

        public void PushPath(DreamPath value) {
            GrowStack(1);
            if (DMObjectTree.TryGetTypeId(value, out int typeId)) {
                WriteOpcode(DreamProcOpcode.PushType);
                WriteInt(typeId);
            } else {
                //TODO: Remove PushPath?
                //It's currently still used by things like paths to procs
                WriteOpcode(DreamProcOpcode.PushPath);
                WriteString(value.PathString);
            }
        }

        public void PushNull() {
            GrowStack(1);
            WriteOpcode(DreamProcOpcode.PushNull);
        }

        public void FormatString(string value) {
            ShrinkStack(value.Count((char c) => c == 0xFF) - 1); //Shrinks by the amount of formats in the string, grows 1
            WriteOpcode(DreamProcOpcode.FormatString);
            WriteString(value);
        }

        public void IsInList() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.IsInList);
        }

        public void IsInRange() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.IsInRange);
        }

        public void IsNull() {
            WriteOpcode(DreamProcOpcode.IsNull);
        }

        public void IsSaved(string propertyName) {
            WriteOpcode(DreamProcOpcode.IsSaved);
            WriteString(propertyName);
        }

        public void IsType() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.IsType);
        }

        public void LocateCoordinates() {
            ShrinkStack(2);
            WriteOpcode(DreamProcOpcode.LocateCoord);
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

        public void Locate() {
            ShrinkStack(1);
            WriteOpcode(DreamProcOpcode.Locate);
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
            int stringID;

            if (!DMObjectTree.StringToStringID.TryGetValue(value, out stringID)) {
                stringID = DMObjectTree.StringTable.Count;

                DMObjectTree.StringTable.Add(value);
                DMObjectTree.StringToStringID.Add(value, stringID);
            }

            WriteInt(stringID);
        }

        private void WriteLabel(string labelName) {
            _unresolvedLabels.Add((Bytecode.Position, labelName));
            WriteInt(0); //Resolved later
        }

        private void WriteReference(DMReference reference, bool affectStack = true) {
            WriteByte((byte)reference.RefType);

            switch (reference.RefType) {
                case DMReference.Type.Local: WriteByte(reference.LocalId); break;
                case DMReference.Type.Global: WriteInt(reference.GlobalId); break;
                case DMReference.Type.Field: WriteString(reference.FieldName); ShrinkStack(affectStack ? 1 : 0); break;
                case DMReference.Type.SrcField: WriteString(reference.FieldName); break;
                case DMReference.Type.Proc: WriteString(reference.ProcName); ShrinkStack(affectStack ? 1 : 0); break;
                case DMReference.Type.GlobalProc: WriteString(reference.ProcName); break;
                case DMReference.Type.SrcProc: WriteString(reference.ProcName); break;
                case DMReference.Type.ListIndex: ShrinkStack(affectStack ? 2 : 0); break;
                case DMReference.Type.SuperProc:
                case DMReference.Type.Src:
                case DMReference.Type.Self:
                case DMReference.Type.Args:
                case DMReference.Type.Usr:
                    break;
                default: throw new CompileErrorException(Location, $"Invalid reference type {reference.RefType}");
            }
        }

        private void GrowStack(int size) {
            _currentStackSize += size;
            _maxStackSize = Math.Max(_currentStackSize, _maxStackSize);
        }

        private void ShrinkStack(int size) {
            _currentStackSize -= size;
            _maxStackSize = Math.Max(_currentStackSize, _maxStackSize);
            if (_currentStackSize < 0) throw new Exception($"Negative stack size {Location}");
        }
    }
}
