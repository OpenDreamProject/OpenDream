using OpenDreamRuntime.Procs;
using System;
using System.Collections.Generic;
using System.Text;
using DMCompiler.DM;
using DMCompiler.Json;
using JetBrains.Annotations;

namespace DMDisassembler;

internal class DMProc(ProcDefinitionJson json) {
    internal struct DecompiledOpcode(int position, string text) {
        public readonly int Position = position;
        public readonly string Text = text;
    }

    public string Name = json.Name;
    public int OwningTypeId = json.OwningTypeId;
    public byte[] Bytecode = json.Bytecode ?? Array.Empty<byte>();
    public bool IsOverride = (json.Attributes & ProcAttributes.IsOverride) != 0;
    public Exception exception;

    public string Decompile() {
        List<DecompiledOpcode> decompiled = GetDecompiledOpcodes(out var labeledPositions);

        StringBuilder result = new StringBuilder();
        foreach (DecompiledOpcode decompiledOpcode in decompiled) {
            if (labeledPositions.Contains(decompiledOpcode.Position)) {
                result.AppendFormat("0x{0:x}", decompiledOpcode.Position);
                result.AppendLine();
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

    public List<DecompiledOpcode> GetDecompiledOpcodes(out HashSet<int> labeledPositions) {
        List<DecompiledOpcode> decompiled = new();
        labeledPositions = new();

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

        return decompiled;
    }

    [CanBeNull]
    public string[] GetArguments() {
        if (json.Arguments is null || json.Arguments.Count == 0) return null;

        string[] argNames = new string[json.Arguments.Count];
        for (var index = 0; index < json.Arguments.Count; index++) {
            argNames[index] = json.Arguments[index].Name;
        }

        return argNames;
    }
}
