using DMCompiler.DM.Visitors;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DMCompiler.DM {
    class DMProc {
        public class DMLocalVariable {
            public int Id;
            public DreamPath? Type;

            public DMLocalVariable(int id, DreamPath? type) {
                Id = id;
                Type = type;
            }
        }

        private class DMProcScope {
            public Dictionary<string, DMLocalVariable> LocalVariables = new();
            public DMProcScope ParentScope;

            public DMProcScope() { }

            public DMProcScope(DMProcScope parentScope) {
                ParentScope = parentScope;
            }
        }

        public MemoryStream Bytecode = new MemoryStream();
        public List<string> Parameters = new();
        public List<DMValueType> ParameterTypes = new();

        private DMASTProcDefinition _astDefinition = null;
        private BinaryWriter _bytecodeWriter = null;
        private Dictionary<string, long> _labels = new();
        private List<(long Position, string LabelName)> _unresolvedLabels = new();
        private Stack<string> _loopStack = new();
        private Stack<DMProcScope> _scopes = new();
        private int _localVariableIdCounter = 0;

        public DMProc(DMASTProcDefinition astDefinition) {
            _astDefinition = astDefinition;
            _bytecodeWriter = new BinaryWriter(Bytecode);
            _scopes.Push(new DMProcScope());
        }

        public void Compile(DMObject dmObject) {
            foreach (DMASTDefinitionParameter parameter in _astDefinition.Parameters) {
                AddParameter(parameter.Name, parameter.Type);
            }

            _astDefinition.Visit(new DMVisitorProcBuilder(dmObject, this));
        }

        public ProcDefinitionJson GetJsonRepresentation() {
            ProcDefinitionJson procDefinition = new ProcDefinitionJson();

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
                    throw new Exception("Invalid label \"" + unresolvedLabel.LabelName + "\"");
                }
            }

            _unresolvedLabels.Clear();
            _bytecodeWriter.Seek(0, SeekOrigin.End);
        }

        public void AddLabel(string name) {
            _labels.Add(name, Bytecode.Position);
        }

        public void AddLocalVariable(string name, DreamPath? type) {
            int localVarId = _localVariableIdCounter++;

            _scopes.Peek().LocalVariables.Add(name, new DMLocalVariable(localVarId, type));
        }

        public DMLocalVariable GetLocalVariable(string name) {
            DMProcScope scope = _scopes.Peek();

            while (scope != null) {
                if (scope.LocalVariables.TryGetValue(name, out DMLocalVariable localVariable)) return localVariable;

                scope = scope.ParentScope;
            }

            return null;
        }

        public void Error() {
            WriteOpcode(DreamProcOpcode.Error);
        }

        public void GetIdentifier(string identifier) {
            WriteOpcode(DreamProcOpcode.GetIdentifier);
            WriteString(identifier);
        }

        public void PushLocalVariable(string name) {
            DMLocalVariable localVar = GetLocalVariable(name);

            WriteOpcode(DreamProcOpcode.PushLocalVariable);
            WriteByte((byte)localVar.Id);
        }

        public void GetProc(string identifier) {
            WriteOpcode(DreamProcOpcode.GetProc);
            WriteString(identifier);
        }

        public void CreateListEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        }
        
        public void CreateRangeEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateRangeEnumerator);
        }

        public void Enumerate(string outputVariableName) {
            WriteOpcode(DreamProcOpcode.Enumerate);
            WriteByte((byte)GetLocalVariable(outputVariableName).Id);
        }

        public void DestroyEnumerator() {
            WriteOpcode(DreamProcOpcode.DestroyEnumerator);
        }

        public void CreateList() {
            WriteOpcode(DreamProcOpcode.CreateList);
        }

        public void ListAppend() {
            WriteOpcode(DreamProcOpcode.ListAppend);
        }

        public void ListAppendAssociated() {
            WriteOpcode(DreamProcOpcode.ListAppendAssociated);
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
            WriteOpcode(DreamProcOpcode.SwitchCase);
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

        public void Break() {
            Jump(_loopStack.Peek() + "_end");
        }
        
        public void BreakIfFalse() {
            JumpIfFalse(_loopStack.Peek() + "_end");
        }

        public void Continue() {
            Jump(_loopStack.Peek() + "_continue");
        }
        
        public void ContinueIfFalse() {
            JumpIfFalse(_loopStack.Peek() + "_continue");
        }

        public void PushProcArguments() {
            WriteOpcode(DreamProcOpcode.PushProcArguments);
        }

        public void PushArgumentList() {
            WriteOpcode(DreamProcOpcode.PushArgumentList);
        }

        public void PushArguments(int argumentCount, DreamProcOpcodeParameterType[] parameterTypes = null, string[] parameterNames = null) {
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
                        WriteString(parameterNames[namedParameterIndex++]);
                    }
                }
            }
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
            _localVariableIdCounter -= destroyedScope.LocalVariables.Count;
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

        public void Call() {
            WriteOpcode(DreamProcOpcode.Call);
        }
        
        public void CallSelf() {
            WriteOpcode(DreamProcOpcode.CallSelf);
        }

        public void CallStatement() {
            WriteOpcode(DreamProcOpcode.CallStatement);
        }

        public void Prompt(DMValueType types) {
            WriteOpcode(DreamProcOpcode.Prompt);
            WriteInt((int)types);
        }

        public void Initial(string propertyName) {
            WriteOpcode(DreamProcOpcode.Initial);
            WriteString(propertyName);
        }

        public void Return() {
            WriteOpcode(DreamProcOpcode.Return);
        }

        public void Assign() {
            WriteOpcode(DreamProcOpcode.Assign);
        }

        public void SetLocalVariable(string name) {
            WriteOpcode(DreamProcOpcode.SetLocalVariable);
            WriteByte((byte)GetLocalVariable(name).Id);
        }

        public void Dereference(string identifier) {
            WriteOpcode(DreamProcOpcode.Dereference);
            WriteString(identifier);
        }

        public void DereferenceProc(string identifier) {
            WriteOpcode(DreamProcOpcode.DereferenceProc);
            WriteString(identifier);
        }

        public void CreateObject() {
            WriteOpcode(DreamProcOpcode.CreateObject);
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

        public void Divide() {
            WriteOpcode(DreamProcOpcode.Divide);
        }
        
        public void Modulus() {
            WriteOpcode(DreamProcOpcode.Modulus);
        }

        public void Power() {
            WriteOpcode(DreamProcOpcode.Power);
        }

        public void Append() {
            WriteOpcode(DreamProcOpcode.Append);
        }

        public void Remove() {
            WriteOpcode(DreamProcOpcode.Remove);
        }

        public void Combine() {
            WriteOpcode(DreamProcOpcode.Combine);
        }

        public void Mask() {
            WriteOpcode(DreamProcOpcode.Mask);
        }

        public void BitShiftLeft() {
            WriteOpcode(DreamProcOpcode.BitShiftLeft);
        }

        public void BitShiftRight() {
            WriteOpcode(DreamProcOpcode.BitShiftRight);
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

        public void BinaryOr() {
            WriteOpcode(DreamProcOpcode.BitOr);
        }

        public void Equal() {
            WriteOpcode(DreamProcOpcode.CompareEquals);
        }

        public void NotEqual() {
            WriteOpcode(DreamProcOpcode.CompareNotEquals);
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

        public void PushSuperProc() {
            WriteOpcode(DreamProcOpcode.PushSuperProc);
        }

        public void PushSelf() {
            WriteOpcode(DreamProcOpcode.PushSelf);
        }

        public void PushSrc() {
            WriteOpcode(DreamProcOpcode.PushSrc);
        }
        
        public void PushUsr() {
            WriteOpcode(DreamProcOpcode.PushUsr);
        }

        public void PushInt(int value) {
            WriteOpcode(DreamProcOpcode.PushInt);
            WriteInt(value);
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
            WriteString(value);
        }

        public void PushPath(DreamPath value) {
            WriteOpcode(DreamProcOpcode.PushPath);
            WriteString(value.PathString);
        }

        public void PushNull() {
            WriteOpcode(DreamProcOpcode.PushNull);
        }

        public void FormatString(string value) {
            WriteOpcode(DreamProcOpcode.FormatString);
            WriteString(value);
        }

        public void IndexList() {
            WriteOpcode(DreamProcOpcode.IndexList);
        }

        public void IsInList() {
            WriteOpcode(DreamProcOpcode.IsInList);
        }

        public void IsNull() {
            WriteOpcode(DreamProcOpcode.IsNull);
        }

        public void IsType() {
            WriteOpcode(DreamProcOpcode.IsType);
        }

        public void LocateCoordinates() {
            WriteOpcode(DreamProcOpcode.LocateCoord);
        }

        public void Locate() {
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
    }
}
