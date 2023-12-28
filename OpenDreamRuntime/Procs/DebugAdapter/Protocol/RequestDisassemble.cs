using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

[UsedImplicitly]
public sealed class RequestDisassemble : Request {
    [JsonPropertyName("arguments")] public required RequestDisassembleArguments Arguments { get; set; }

    [UsedImplicitly]
    public sealed class RequestDisassembleArguments {
        /**
         * Memory reference to the base location containing the instructions to
         * disassemble.
         */
        [JsonPropertyName("memoryReference")] public required string MemoryReference { get; set; }

        /**
         * Offset (in bytes) to be applied to the reference location before
         * disassembling. Can be negative.
         */
        [JsonPropertyName("offset")] public int? Offset { get; set; }

        /**
         * Offset (in instructions) to be applied after the byte offset (if any)
         * before disassembling. Can be negative.
         */
        [JsonPropertyName("instructionOffset")] public int? InstructionOffset { get; set; }

        /**
         * Number of instructions to disassemble starting at the specified location
         * and offset.
         * An adapter must return exactly this number of instructions - any
         * unavailable instructions should be replaced with an implementation-defined
         * 'invalid instruction' value.
         */
        [JsonPropertyName("instructionCount")] public int InstructionCount { get; set; }

        /**
         * If true, the adapter should attempt to resolve memory addresses and other
         * values to symbolic names.
         */
        [JsonPropertyName("resolveSymbols")] public bool? ResolveSymbols { get; set; }
    }

    public void Respond(DebugAdapterClient client, IEnumerable<DisassembledInstruction> instructions) {
        client.SendMessage(Response.NewSuccess(this, new { instructions }));
    }
}
