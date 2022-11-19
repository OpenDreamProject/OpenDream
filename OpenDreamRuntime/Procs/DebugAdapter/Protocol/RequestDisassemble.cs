using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class RequestDisassemble : Request {
    [JsonPropertyName("arguments")] public RequestDisassembleArguments Arguments { get; set; }

    public sealed class RequestDisassembleArguments {
        /**
         * Memory reference to the base location containing the instructions to
         * disassemble.
         */
        [JsonPropertyName("memoryReference")] public string MemoryReference { get; set; }

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
        [JsonPropertyName("instructionCount")] public int instructionCount { get; set; }

        /**
         * If true, the adapter should attempt to resolve memory addresses and other
         * values to symbolic names.
         */
        [JsonPropertyName("resolveSymbols")] public bool? resolveSymbols { get; set; }
    }

    public void Respond(DebugAdapterClient client, IEnumerable<DisassembledInstruction> instructions) {
        client.SendMessage(Response.NewSuccess(this, new { instructions }));
    }
}
