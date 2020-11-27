using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DMCompiler.DM {
    class DMProc {
        public struct Parameter {
            public string Name;
            public object DefaultValue;

            public Parameter(string name, object defaultValue) {
                Name = name;
                DefaultValue = defaultValue;
            }
        }

        public MemoryStream Bytecode = new MemoryStream();
        public List<Parameter> Parameters = new List<Parameter>();

        private BinaryWriter _bytecodeWriter = null;
        private Dictionary<string, long> _labels = new Dictionary<string, long>();
        private List<(long Position, string LabelName)> _unresolvedLabels = new List<(long, string)>();
        private Stack<string> _loopStack = new Stack<string>();

        public DMProc() {
            _bytecodeWriter = new BinaryWriter(Bytecode);
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
            WriteOpcode(DreamProcOpcode.GetIdentifier);
            WriteString(identifier);
        }

        public void CreateListEnumerator() {
            WriteOpcode(DreamProcOpcode.CreateListEnumerator);
        }

        public void EnumerateList(string outputVariableName) {
            WriteOpcode(DreamProcOpcode.EnumerateList);
            WriteString(outputVariableName);
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
            CreateScope();
        }

        public void LoopEnd() {
            AddLabel(_loopStack.Pop() + "_end");
            DestroyScope();
        }

        public void SwitchCase(string caseLabel) {
            WriteOpcode(DreamProcOpcode.SwitchCase);
            WriteLabel(caseLabel);
        }

        public void Break() {
            Jump(_loopStack.Peek() + "_end");
        }

        public void Continue() {
            DestroyScope();
            Jump(_loopStack.Peek() + "_start");
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

        public void CallStatement() {
            WriteOpcode(DreamProcOpcode.CallStatement);
        }

        public void Return() {
            WriteOpcode(DreamProcOpcode.Return);
        }

        public void Assign() {
            WriteOpcode(DreamProcOpcode.Assign);
        }

        public void DefineVariable(string name) {
            WriteOpcode(DreamProcOpcode.DefineVariable);
            WriteString(name);
        }

        public void Dereference(string identifier) {
            WriteOpcode(DreamProcOpcode.Dereference);
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

        public void PushDouble(double value) {
            WriteOpcode(DreamProcOpcode.PushDouble);
            WriteDouble(value);
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

        public void BuildString(int pieceCount) {
            WriteOpcode(DreamProcOpcode.BuildString);
            WriteInt(pieceCount);
        }

        public void IndexList() {
            WriteOpcode(DreamProcOpcode.IndexList);
        }

        public void IsInList() {
            WriteOpcode(DreamProcOpcode.IsInList);
        }

        private void WriteOpcode(DreamProcOpcode opcode) {
            _bytecodeWriter.Write((byte)opcode);
        }

        private void WriteInt(int value) {
            if (value > Int32.MaxValue && value < Int32.MinValue) throw new ArgumentOutOfRangeException("Integer cannot be represented by an Int32");

            _bytecodeWriter.Write((byte)((value & 0xFF000000) >> 24));
            _bytecodeWriter.Write((byte)((value & 0x00FF0000) >> 16));
            _bytecodeWriter.Write((byte)((value & 0x0000FF00) >> 8));
            _bytecodeWriter.Write((byte)(value & 0x000000FF));
        }

        private void WriteDouble(double value) {
            _bytecodeWriter.Write(value);
        }

        private void WriteString(string value) {
            foreach (char character in value.ToCharArray()) {
                _bytecodeWriter.Write(character);
            }

            _bytecodeWriter.Write((byte)0);
        }

        private void WriteLabel(string labelName) {
            _unresolvedLabels.Add((Bytecode.Position, labelName));
            WriteInt(0); //Resolved later
        }
    }
}
