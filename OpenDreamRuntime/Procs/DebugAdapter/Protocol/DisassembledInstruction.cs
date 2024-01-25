using System.Text.Json.Serialization;

namespace OpenDreamRuntime.Procs.DebugAdapter.Protocol;

public sealed class DisassembledInstruction {
    /**
     * The address of the instruction. Treated as a hex value if prefixed with
     * `0x`, or as a decimal value otherwise.
     */
    [JsonPropertyName("address")] public required string Address { get; set; }

    /**
     * Raw bytes representing the instruction and its operands, in an
     * implementation-defined format.
     */
    [JsonPropertyName("instructionBytes")] public string? InstructionBytes { get; set; }

    /**
     * Text representing the instruction and its operands, in an
     * implementation-defined format.
     */
    [JsonPropertyName("instruction")] public required string Instruction { get; set; }

    /**
     * Name of the symbol that corresponds with the location of this instruction,
     * if any.
     */
    [JsonPropertyName("symbol")] public string? Symbol { get; set; }

    /**
     * Source location that corresponds to this instruction, if any.
     * Should always be set (if available) on the first instruction returned,
     * but can be omitted afterwards if this instruction maps to the same source
     * file as the previous instruction.
     */
    [JsonPropertyName("location")] public Source? Location { get; set; }

    /**
     * The line within the source location that corresponds to this instruction,
     * if any.
     */
    [JsonPropertyName("line")] public int? Line { get; set; }

    /**
     * The column within the line that corresponds to this instruction, if any.
     */
    [JsonPropertyName("column")] public int? Column { get; set; }

    /**
     * The end line of the range that corresponds to this instruction, if any.
     */
    [JsonPropertyName("endLine")] public int? EndLine { get; set; }

    /**
     * The end column of the range that corresponds to this instruction, if any.
     */
    [JsonPropertyName("endColumn")] public int? EndColumn { get; set; }
}
