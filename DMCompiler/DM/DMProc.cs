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

        public DMProc() {
            _bytecodeWriter = new BinaryWriter(Bytecode);
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

        public void Call() {
            WriteOpcode(DreamProcOpcode.Call);
        }

        public void Return() {
            WriteOpcode(DreamProcOpcode.Return);
        }

        public void Assign() {
            WriteOpcode(DreamProcOpcode.Assign);
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
