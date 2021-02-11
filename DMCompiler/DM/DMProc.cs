using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.IO;

namespace DMCompiler.DM {
    class DMProc {
        private class DMProcScope {
            public Dictionary<string, int> LocalVariables = new();
        }

        public MemoryStream Bytecode = new MemoryStream();
        public List<string> Parameters = new();

        private BinaryWriter _bytecodeWriter = null;
        private Dictionary<string, long> _labels = new();
        private List<(long Position, string LabelName)> _unresolvedLabels = new();
        private Stack<string> _loopStack = new();
        private Stack<DMProcScope> _scopes = new();
        private int _localVariableIdCounter = 0;

        public DMProc() {
            _bytecodeWriter = new BinaryWriter(Bytecode);
            _scopes.Push(new DMProcScope());
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

        public void Error() {
            WriteOpcode(DreamProcOpcode.Error);
        }

        public void GetIdentifier(string identifier) {
            int localVarId = GetLocalVariableId(identifier);

            if (localVarId != -1) {
                WriteOpcode(DreamProcOpcode.GetLocalVariable);
                WriteByte((byte)localVarId);
            } else {
                WriteOpcode(DreamProcOpcode.GetIdentifier);
                WriteString(identifier);
            }
        }

        public void GetProc(string identifier) {
            WriteOpcode(DreamProcOpcode.GetProc);
            WriteString(identifier);
        }

        public void CreateListEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        }

        public void EnumerateList(string outputVariableName) {
            WriteOpcode(DreamProcOpcode.EnumerateList);
            WriteByte((byte)GetLocalVariableId(outputVariableName));
        }

        public void DestroyListEnumerator() {
            WriteOpcode(DreamProcOpcode.DestroyListEnumerator);
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
            DestroyScope();
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

        public void Continue() {
            Jump(_loopStack.Peek() + "_continue");
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
            CreateScope();
            _scopes.Push(new DMProcScope());
        }

        public void EndScope() {
            DestroyScope();

            DMProcScope destroyedScope = _scopes.Pop();
            _localVariableIdCounter -= destroyedScope.LocalVariables.Count;
        }

        public void CreateScope() {
            WriteOpcode(DreamProcOpcode.CreateScope);
        }

        public void DestroyScope() {
            WriteOpcode(DreamProcOpcode.DestroyScope);
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

        public void Initial() {
            WriteOpcode(DreamProcOpcode.Initial);
        }

        public void Return() {
            WriteOpcode(DreamProcOpcode.Return);
        }

        public void Assign() {
            WriteOpcode(DreamProcOpcode.Assign);
        }

        public void SetLocalVariable(string name) {
            WriteOpcode(DreamProcOpcode.SetLocalVariable);
            WriteByte((byte)GetLocalVariableId(name, true));
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

        public void IsType() {
            WriteOpcode(DreamProcOpcode.IsType);
        }

        private void WriteOpcode(DreamProcOpcode opcode) {
            _bytecodeWriter.Write((byte)opcode);
        }

        private int GetLocalVariableId(string name, bool create = false) {
            int localVarId = -1;
            foreach (DMProcScope scope in _scopes) {
                if (scope.LocalVariables.TryGetValue(name, out int foundLocalVarId)) {
                    localVarId = foundLocalVarId;
                    break;
                }
            }

            if (localVarId == -1 && create) {
                localVarId = _localVariableIdCounter++;
                _scopes.Peek().LocalVariables.Add(name, localVarId);
            }

            return localVarId;
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

            if (!Program.StringToStringID.TryGetValue(value, out stringID)) {
                stringID = Program.StringTable.Count;

                Program.StringTable.Add(value);
                Program.StringToStringID.Add(value, stringID);
            }

            WriteInt(stringID);
        }

        private void WriteLabel(string labelName) {
            _unresolvedLabels.Add((Bytecode.Position, labelName));
            WriteInt(0); //Resolved later
        }
    }
}
