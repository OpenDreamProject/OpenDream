using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream.Procs;
using OpenDreamShared.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DMDisassembler {
    class DMProc {
        private class DecompiledOpcode {
            public int Position;
            public string Text;

            public DecompiledOpcode(int position, string text) {
                Position = position;
                Text = text;
            }
        }

        public string Name;
        public byte[] Bytecode;
        public Exception exception;

        public DMProc(ProcDefinitionJson json) {
            Name = json.Name;
            Bytecode = json.Bytecode ?? Array.Empty<byte>();
        }

        public string Decompile() {
            List<DecompiledOpcode> decompiled = new();
            HashSet<int> labeledPositions = new();

            try {
                foreach (var (position, instruction) in new ProcDecoder(Program.CompiledJson.Strings, Bytecode).Disassemble()) {
                    StringBuilder text = new StringBuilder();
                    text.Append(instruction[0]);
                    text.Append(" ");

                    switch (instruction) {
                        case (DreamProcOpcode.FormatString, string str, int numReplacements):
                            text.Append(numReplacements);
                            text.Append(' ');
                            text.Append('"');
                            text.Append(str);
                            text.Append('"');
                            break;

                        case (DreamProcOpcode.PushString, string str):
                            text.Append('"');
                            text.Append(str);
                            text.Append('"');
                            break;

                        case (DreamProcOpcode.PushResource, string str):
                            text.Append('\'');
                            text.Append(str);
                            text.Append('\'');
                            break;

                        case (DreamProcOpcode.JumpIfNullDereference, DMReference reference, int jumpPosition):
                            labeledPositions.Add(jumpPosition);
                            text.Append(reference);
                            text.Append(" ");
                            text.Append(jumpPosition);
                            break;

                        case (DreamProcOpcode.Spawn
                                or DreamProcOpcode.BooleanOr
                                or DreamProcOpcode.BooleanAnd
                                or DreamProcOpcode.SwitchCase
                                or DreamProcOpcode.SwitchCaseRange
                                or DreamProcOpcode.Jump
                                or DreamProcOpcode.JumpIfFalse
                                or DreamProcOpcode.JumpIfTrue
                                or DreamProcOpcode.JumpIfNullNoPop
                                or DreamProcOpcode.JumpIfTrueReferenceNoPop
                                or DreamProcOpcode.JumpIfFalseReferenceNoPop, int jumpPosition):
                            labeledPositions.Add(jumpPosition);
                            text.Append(jumpPosition);
                            break;

                        case (DreamProcOpcode.PushType, int type):
                            text.Append(Program.CompiledJson.Types[type].Path);
                            break;

                        case (DreamProcOpcode.PushArguments, int argCount, int namedCount, string[] names):
                            text.Append(argCount);
                            for (int i = 0; i < argCount; i++) {
                                text.Append(" ");
                                text.Append(names[i] ?? "-");
                            }

                            break;

                        default:
                            for (int i = 1; i < instruction.Length; ++i) {
                                text.Append(instruction[i]);
                                text.Append(" ");
                            }
                            break;
                    }
                    decompiled.Add(new DecompiledOpcode(position, text.ToString()));
                }
            } catch (Exception ex) {
                exception = ex;
            }

            StringBuilder result = new StringBuilder();
            foreach (DecompiledOpcode decompiledOpcode in decompiled) {
                if (labeledPositions.Contains(decompiledOpcode.Position)) {
                    result.Append(decompiledOpcode.Position);
                }
                result.Append('\t');
                result.AppendLine(decompiledOpcode.Text);
            }

            if (labeledPositions.Contains(Bytecode.Length)) {
                // In case of a Jump off the end of the proc.
                result.Append(Bytecode.Length);
                result.AppendLine();
            }

            if (exception != null) {
                result.Append(exception);
            }

            return result.ToString();
        }
    }
}
