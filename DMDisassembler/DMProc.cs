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

            var decoder = new ProcDecoder(Program.CompiledJson.Strings, Bytecode);
            while (decoder.Remaining > 0) {
                long position = decoder.Offset;
                var opcode = decoder.ReadOpcode();
                StringBuilder text = new StringBuilder();
                text.Append(opcode);
                text.Append(" ");

                switch (opcode) {
                    case DreamProcOpcode.FormatString: {
                        text.Append('"');
                        text.Append(decoder.ReadString());
                        text.Append('"');
                        decoder.ReadInt(); // This is some metadata FormatString has that we can't really render

                        break;
                    }
                    case DreamProcOpcode.PushString: {
                        text.Append('"');
                        text.Append(decoder.ReadString());
                        text.Append('"');

                        break;
                    }

                    case DreamProcOpcode.PushResource: {
                        text.Append('\'');
                        text.Append(decoder.ReadString());
                        text.Append('\'');

                        break;
                    }

                    case DreamProcOpcode.Prompt:
                        text.Append(decoder.ReadValueType());
                        break;

                    case DreamProcOpcode.PushFloat:
                        text.Append(decoder.ReadFloat());
                        break;

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
                    case DreamProcOpcode.OutputReference:
                    case DreamProcOpcode.PushReferenceValue:
                        text.Append(decoder.ReadReference());
                        break;

                    case DreamProcOpcode.Input:
                        text.Append(decoder.ReadReference());
                        text.Append(decoder.ReadReference());
                        break;

                    case DreamProcOpcode.CreateList:
                    case DreamProcOpcode.CreateAssociativeList:
                    case DreamProcOpcode.PickWeighted:
                    case DreamProcOpcode.PickUnweighted:
                        text.Append(decoder.ReadInt());
                        break;

                    case DreamProcOpcode.JumpIfNullDereference: {
                        DMReference reference = decoder.ReadReference();
                        int jumpPosition = decoder.ReadInt();

                        labeledPositions.Add(jumpPosition);
                        text.Append(reference.ToString());
                        text.Append(" ");
                        text.Append(jumpPosition);
                        break;
                    }

                    case DreamProcOpcode.Initial:
                    case DreamProcOpcode.IsSaved:
                    case DreamProcOpcode.PushPath:
                        text.Append(decoder.ReadString());
                        break;

                    case DreamProcOpcode.Spawn:
                    case DreamProcOpcode.BooleanOr:
                    case DreamProcOpcode.BooleanAnd:
                    case DreamProcOpcode.SwitchCase:
                    case DreamProcOpcode.SwitchCaseRange:
                    case DreamProcOpcode.Jump:
                    case DreamProcOpcode.JumpIfFalse:
                    case DreamProcOpcode.JumpIfTrue: {
                        int jumpPosition = decoder.ReadInt();

                        labeledPositions.Add(jumpPosition);
                        text.Append(jumpPosition);
                        break;
                    }

                    case DreamProcOpcode.PushType:
                        text.Append(Program.CompiledJson.Types[decoder.ReadInt()].Path);
                        break;

                    case DreamProcOpcode.PushArguments: {
                        int argCount = decoder.ReadInt();
                        int namedCount = decoder.ReadInt();

                        text.Append(argCount);
                        for (int i = 0; i < argCount; i++) {
                            text.Append(" ");

                            DreamProcOpcodeParameterType argType = decoder.ReadParameterType();
                            if (argType == DreamProcOpcodeParameterType.Named) {
                                string argName = decoder.ReadString();

                                text.Append("Named(" + argName + ")");
                            } else {
                                text.Append("Unnamed");
                            }
                        }

                        break;
                    }
                }

                decompiled.Add(new DecompiledOpcode(position, text.ToString()));
            }

            StringBuilder result = new StringBuilder();
            foreach (DecompiledOpcode decompiledOpcode in decompiled) {
                if (labeledPositions.Contains((int)decompiledOpcode.Position)) result.Append(decompiledOpcode.Position);

                result.Append('\t');
                result.AppendLine(decompiledOpcode.Text);
            }

            return result.ToString();
        }
    }
}
