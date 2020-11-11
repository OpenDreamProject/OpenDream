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
        private int _labelIdCounter = 0;

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

            _bytecodeWriter.Seek(0, SeekOrigin.End);
        }

        public void AddLabel(string name) {
            _labels.Add(name, Bytecode.Position);
        }

        public void GetIdentifier(string identifier) {
            WriteOpcode(DreamProcOpcode.GetIdentifier);
            WriteString(identifier);
        }

        public void PushArguments(int argumentCount, DreamProcOpcodeParameterType[] parameterTypes = null, string[] parameterNames = null) {
            if ((argumentCount > 0 && parameterTypes == null) || parameterTypes.Length != argumentCount) {
                throw new ArgumentException("Length of parameter types does not match the argument count");
            }

            WriteOpcode(DreamProcOpcode.PushArguments);
            WriteInt(argumentCount);
            if (parameterTypes != null) {
                int namedParameterIndex = 0;

                foreach (DreamProcOpcodeParameterType parameterType in parameterTypes) {
                    _bytecodeWriter.Write((byte)parameterType);

                    if (parameterType == DreamProcOpcodeParameterType.Named) {
                        WriteString(parameterNames[namedParameterIndex++]);
                    }
                }
            }
        }

        public string IfStart() {
            string endLabel = "label" + _labelIdCounter++;

            JumpIfFalse(endLabel);
            WriteOpcode(DreamProcOpcode.CreateScope);

            return endLabel;
        }

        public void IfEnd(string endLabelName) {
            WriteOpcode(DreamProcOpcode.DestroyScope);
            AddLabel(endLabelName);
        }

        public (string ElseLabel, string EndLabel) IfElseStart() {
            string elseLabel = "label" + _labelIdCounter++;
            string endLabel = "label" + _labelIdCounter++;

            JumpIfFalse(elseLabel);
            WriteOpcode(DreamProcOpcode.CreateScope);

            return (elseLabel, endLabel);
        }

        public void IfElseEnd(string elseLabel, string endLabel) {
            WriteOpcode(DreamProcOpcode.DestroyScope);
            Jump(endLabel);

            AddLabel(elseLabel);
        }

        public void Jump(string label) {
            WriteOpcode(DreamProcOpcode.Jump);

            _unresolvedLabels.Add((Bytecode.Position, label));
            WriteInt(0); //Resolved later
        }

        public void JumpIfFalse(string label) {
            WriteOpcode(DreamProcOpcode.JumpIfFalse);

            _unresolvedLabels.Add((Bytecode.Position, label));
            WriteInt(0); //Resolved later
        }

        public void Call() {
            WriteOpcode(DreamProcOpcode.Call);
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

        public void Add() {
            WriteOpcode(DreamProcOpcode.Add);
        }

        public void Subtract() {
            WriteOpcode(DreamProcOpcode.Subtract);
        }

        public void BitShiftLeft() {
            WriteOpcode(DreamProcOpcode.BitShiftLeft);
        }

        public void BinaryAnd() {
            WriteOpcode(DreamProcOpcode.BitAnd);
        }

        public void Equal() {
            WriteOpcode(DreamProcOpcode.CompareEquals);
        }

        public void GreaterThan() {
            WriteOpcode(DreamProcOpcode.CompareGreaterThan);
        }

        public void LessThan() {
            WriteOpcode(DreamProcOpcode.CompareLessThan);
        }

        public void PushSuperProc() {
            WriteOpcode(DreamProcOpcode.PushSuperProc);
        }

        public void PushInt(int value) {
            WriteOpcode(DreamProcOpcode.PushInt);
            WriteInt(value);
        }

        public void PushString(string value) {
            WriteOpcode(DreamProcOpcode.PushString);
            WriteString(value);
        }

        public void PushNull() {
            WriteOpcode(DreamProcOpcode.PushNull);
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

        private void WriteString(string value) {
            foreach (char character in value.ToCharArray()) {
                _bytecodeWriter.Write(character);
            }

            _bytecodeWriter.Write((byte)0);
        }
    }
}
