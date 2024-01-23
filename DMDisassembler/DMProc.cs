using OpenDreamRuntime.Procs;
using System;
using System.Collections.Generic;
using System.Text;
using DMCompiler.Json;

namespace DMDisassembler;

internal class DMProc(ProcDefinitionJson json) {
    private class DecompiledOpcode(int position, string text) {
        public readonly int Position = position;
        public readonly string Text = text;
    }

    public string Name = json.Name;
    public byte[] Bytecode = json.Bytecode ?? Array.Empty<byte>();
    public Exception exception;

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
