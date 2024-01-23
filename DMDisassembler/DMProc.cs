using OpenDreamRuntime.Procs;
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
                    decompiled.Add(new DecompiledOpcode(position, ProcDecoder.Format(instruction, type => Program.CompiledJson.Types[type].Path)));
                    if (ProcDecoder.GetJumpDestination(instruction) is int jumpPosition) {
                        labeledPositions.Add(jumpPosition);
                    }
                }
            } catch (Exception ex) {
                exception = ex;
            }

            StringBuilder result = new StringBuilder();
            foreach (DecompiledOpcode decompiledOpcode in decompiled) {
                if (labeledPositions.Contains(decompiledOpcode.Position)) {
                    result.AppendFormat("0x{0:x}", decompiledOpcode.Position);
                }
                result.Append('\t');
                result.AppendLine(decompiledOpcode.Text);
            }

            if (labeledPositions.Contains(Bytecode.Length)) {
                // In case of a Jump off the end of the proc.
                result.AppendFormat("0x{0:x}", Bytecode.Length);
                result.AppendLine();
            }

            if (exception != null) {
                result.Append(exception);
            }

            return result.ToString();
        }
    }
}
