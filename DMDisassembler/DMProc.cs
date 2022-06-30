using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DMDisassembler {
    class DMProc {
        private class DecompiledOpcode {
            public long Position;
            public string Text;

            public DecompiledOpcode(long position, string text) {
                Position = position;
                Text = text;
            }
        }

        public string Name;
        public byte[] Bytecode;

        public DMProc(ProcDefinitionJson json) {
            Name = json.Name;
            Bytecode = (json.Bytecode != null) ? json.Bytecode : Array.Empty<byte>();
        }

        public string Decompile() {
            List<DecompiledOpcode> decompiled = new();
            HashSet<int> labeledPositions = new();

            MemoryStream stream = new MemoryStream(Bytecode);
            BinaryReader binaryReader = new BinaryReader(stream);
            long position = 0;
            DreamProcOpcode opcode;
            while ((opcode = (DreamProcOpcode)stream.ReadByte()) != (DreamProcOpcode)(-1)) {
                StringBuilder text = new StringBuilder();
                text.Append(opcode);
                text.Append(" ");

                switch (opcode) {
                    case DreamProcOpcode.FormatString:
                    case DreamProcOpcode.PushString: {
                        text.Append('"');
                        text.Append(Program.CompiledJson.Strings[binaryReader.ReadInt32()]);
                        text.Append('"');

                        break;
                    }

                    case DreamProcOpcode.PushResource: {
                        text.Append('\'');
                        text.Append(Program.CompiledJson.Strings[binaryReader.ReadInt32()]);
                        text.Append('\'');

                        break;
                    }

                    case DreamProcOpcode.Prompt: text.Append((DMValueType)binaryReader.ReadInt32()); break;

                    case DreamProcOpcode.PushFloat: text.Append(binaryReader.ReadSingle()); break;

                    case DreamProcOpcode.Call:
                    case DreamProcOpcode.Assign:
                    case DreamProcOpcode.Append:
                    case DreamProcOpcode.Remove:
                    case DreamProcOpcode.Combine:
                    case DreamProcOpcode.Increment:
                    case DreamProcOpcode.Decrement:
                    case DreamProcOpcode.Mask:
                    case DreamProcOpcode.MultiplyReference:
                    case DreamProcOpcode.DivideReference:
                    case DreamProcOpcode.BitXorReference:
                    case DreamProcOpcode.Enumerate:
                    case DreamProcOpcode.PushReferenceValue: text.Append(ReadReference(binaryReader).ToString()); break;

                    case DreamProcOpcode.CreateList:
                    case DreamProcOpcode.CreateAssociativeList:
                    case DreamProcOpcode.PickWeighted:
                    case DreamProcOpcode.PickUnweighted: text.Append(binaryReader.ReadInt32()); break;

                    case DreamProcOpcode.JumpIfNullDereference: {
                        DMReference reference = ReadReference(binaryReader);
                        int jumpPosition = binaryReader.ReadInt32();

                        labeledPositions.Add(jumpPosition);
                        text.Append(reference.ToString());
                        text.Append(" ");
                        text.Append(jumpPosition);
                        break;
                    }

                    case DreamProcOpcode.Initial:
                    case DreamProcOpcode.IsSaved:
                    case DreamProcOpcode.PushPath: text.Append(Program.CompiledJson.Strings[binaryReader.ReadInt32()]); break;

                    case DreamProcOpcode.Spawn:
                    case DreamProcOpcode.BooleanOr:
                    case DreamProcOpcode.BooleanAnd:
                    case DreamProcOpcode.SwitchCase:
                    case DreamProcOpcode.SwitchCaseRange:
                    case DreamProcOpcode.Jump:
                    case DreamProcOpcode.JumpIfFalse:
                    case DreamProcOpcode.JumpIfTrue: {
                        int jumpPosition = binaryReader.ReadInt32();

                        labeledPositions.Add(jumpPosition);
                        text.Append(jumpPosition);
                        break;
                    }

                    case DreamProcOpcode.PushType: text.Append(Program.CompiledJson.Types[binaryReader.ReadInt32()].Path); break;

                    case DreamProcOpcode.PushArguments: {
                        int argCount = binaryReader.ReadInt32();
                        int namedCount = binaryReader.ReadInt32();

                        text.Append(argCount);
                        for (int i = 0; i < argCount; i++) {
                            text.Append(" ");

                            DreamProcOpcodeParameterType argType = (DreamProcOpcodeParameterType)binaryReader.ReadByte();
                            if (argType == DreamProcOpcodeParameterType.Named) {
                                string argName = Program.CompiledJson.Strings[binaryReader.ReadInt32()];

                                text.Append("Named(" + argName + ")");
                            } else {
                                text.Append("Unnamed");
                            }
                        }

                        break;
                    }
                }

                decompiled.Add(new DecompiledOpcode(position, text.ToString()));
                position = stream.Position;
            }

            StringBuilder result = new StringBuilder();
            foreach (DecompiledOpcode decompiledOpcode in decompiled) {
                if (labeledPositions.Contains((int)decompiledOpcode.Position)) result.Append(decompiledOpcode.Position);

                result.Append('\t');
                result.AppendLine(decompiledOpcode.Text);
            }

            return result.ToString();
        }

        private DMReference ReadReference(BinaryReader reader) {
            DMReference.Type refType = (DMReference.Type)reader.ReadByte();

            switch (refType) {
                case DMReference.Type.Argument: return DMReference.CreateArgument(reader.ReadByte());
                case DMReference.Type.Local: return DMReference.CreateLocal(reader.ReadByte());
                case DMReference.Type.Global: return DMReference.CreateGlobal(reader.ReadInt32());
                case DMReference.Type.GlobalProc: return DMReference.CreateGlobalProc(reader.ReadInt32());
                case DMReference.Type.Field: return DMReference.CreateField(Program.CompiledJson.Strings[reader.ReadInt32()]);
                case DMReference.Type.SrcField: return DMReference.CreateSrcField(Program.CompiledJson.Strings[reader.ReadInt32()]);
                case DMReference.Type.Proc: return DMReference.CreateProc(Program.CompiledJson.Strings[reader.ReadInt32()]);
                case DMReference.Type.SrcProc: return DMReference.CreateSrcProc(Program.CompiledJson.Strings[reader.ReadInt32()]);
                default: return new DMReference() { RefType = refType };
            }
        }
    }
}
