using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using DMCompiler.DM;

// ReSharper disable UnusedMember.Global

namespace DMCompiler.Bytecode;

/// <summary>
/// A typed bytecode operand. Array operands are used only by compact variable-argument opcodes
/// TODO: When .NET 11 is out, revisit this with C# 15 unions and OneOrMany
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ProcOperand {
    public readonly int Int;
    public readonly float Float;
    public readonly string? String;
    public readonly DMReference Reference;
    public readonly DMReference[]? References;
    public readonly float[]? Floats;
    public readonly string[]? Strings;

    private ProcOperand(int intValue = default, float floatValue = default, string? stringValue = default,
        DMReference reference = default, DMReference[]? references = default, float[]? floats = default,
        string[]? strings = default) {
        Int = intValue;
        Float = floatValue;
        String = stringValue;
        Reference = reference;
        References = references;
        Floats = floats;
        Strings = strings;
    }

    public static ProcOperand FromInt(int value) {
        return new ProcOperand(intValue: value);
    }

    public static ProcOperand FromFloat(float value) {
        return new ProcOperand(floatValue: value);
    }

    public static ProcOperand FromString(string value) {
        return new ProcOperand(stringValue: value);
    }

    public static ProcOperand FromReference(DMReference value) {
        return new ProcOperand(reference: value);
    }

    public static ProcOperand FromReferences(DMReference[] values) {
        return new ProcOperand(references: values);
    }

    public static ProcOperand FromFloats(float[] values) {
        return new ProcOperand(floats: values);
    }

    public static ProcOperand FromStrings(string[] values) {
        return new ProcOperand(strings: values);
    }
}

/// <summary>
/// A decoded instruction from a proc bytecode stream
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct ProcInstruction(
    int offset,
    int endOffset,
    DreamProcOpcode opcode,
    int operandCount = 0,
    ProcOperandShape operandShape = ProcOperandShape.Fixed,
    ProcOperand operand0 = default,
    ProcOperand operand1 = default,
    ProcOperand operand2 = default,
    ProcOperand operand3 = default) {
    public readonly int Offset = offset;

    public readonly int EndOffset = endOffset; // TODO: CFG exporter will use this
    public readonly DreamProcOpcode Opcode = opcode;
    public readonly int OperandCount = operandCount;

    private ProcOperand GetOperand(int index) {
        return index switch {
            0 => operand0,
            1 => operand1,
            2 => operand2,
            3 => operand3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
    }

    public OpcodeArgType GetOperandType(int index) {
        OpcodeMetadata metadata = OpcodeMetadataCache.GetMetadata(Opcode);
        return operandShape == ProcOperandShape.Fixed ? metadata.RequiredArgs[index] : metadata.RepeatedArgs[index];
    }

    public int GetInt(int index) {
        return GetOperand(index).Int;
    }

    public float GetFloat(int index) {
        return GetOperand(index).Float;
    }

    public string GetString(int index) {
        return GetOperand(index).String!;
    }

    public DMReference GetReference(int index) {
        return GetOperand(index).Reference;
    }

    public DMReference[] GetReferences(int index) {
        return GetOperand(index).References!;
    }

    public float[] GetFloats(int index) {
        return GetOperand(index).Floats!;
    }

    public string[] GetStrings(int index) {
        return GetOperand(index).Strings!;
    }

    public string Format(Func<int, string> getTypePath) {
        var text = new StringBuilder();
        text.Append(Opcode);

        if (operandShape == ProcOperandShape.Fixed) {
            OpcodeArgType[] operandTypes = OpcodeMetadataCache.GetMetadata(Opcode).RequiredArgs;
            for (int i = 0; i < OperandCount; i++) {
                text.Append(' ');
                AppendOperand(text, operandTypes[i], GetOperand(i), getTypePath);
            }
        } else {
            AppendRepeatedOperands(text);
        }

        return text.ToString();
    }

    public int? GetJumpDestination() {
        int labelIndex = OpcodeMetadataCache.GetMetadata(Opcode).JumpDestinationOperandIndex;

        return labelIndex == -1 ? null : GetInt(labelIndex);
    }

    private void AppendRepeatedOperands(StringBuilder text) {
        switch (operandShape) {
            case ProcOperandShape.RepeatedFloat: {
                float[] values = operand0.Floats!;
                foreach (var t in values)
                {
                    text.Append(' ');
                    AppendFloat(text, t);
                }

                break;
            }
            case ProcOperandShape.RepeatedString: {
                string[] values = operand0.Strings!;
                for (int i = 0; i < values.Length; i++) {
                    text.Append(' ');
                    AppendQuoted(text, values[i]);
                }

                break;
            }
            case ProcOperandShape.RepeatedResource: {
                string[] values = operand0.Strings!;
                for (int i = 0; i < values.Length; i++) {
                    text.Append(' ');
                    AppendResource(text, values[i]);
                }

                break;
            }
            case ProcOperandShape.RepeatedReference: {
                DMReference[] values = operand0.References!;
                for (int i = 0; i < values.Length; i++) {
                    text.Append(' ');
                    AppendReference(text, values[i]);
                }

                break;
            }
            case ProcOperandShape.RepeatedStringFloat: {
                string[] strings = operand0.Strings!;
                float[] floats = operand1.Floats!;
                for (int i = 0; i < strings.Length; i++) {
                    text.Append(' ');
                    AppendQuoted(text, strings[i]);
                    text.Append(' ');
                    AppendFloat(text, floats[i]);
                }

                break;
            }
            case ProcOperandShape.RepeatedFloatReference: {
                float[] floats = operand0.Floats!;
                DMReference[] references = operand1.References!;
                for (int i = 0; i < floats.Length; i++) {
                    text.Append(' ');
                    AppendFloat(text, floats[i]);
                    text.Append(' ');
                    AppendReference(text, references[i]);
                }

                break;
            }
            default:
                throw new InvalidOperationException($"Unsupported repeated operand shape {operandShape} for opcode {Opcode}");
        }
    }

    private static void AppendOperand(StringBuilder text, OpcodeArgType argType, ProcOperand operand,
        Func<int, string> getTypePath) {
        switch (argType) {
            case OpcodeArgType.ArgType:
                text.Append((DMCallArgumentsType)operand.Int);
                break;
            case OpcodeArgType.Float:
                AppendFloat(text, operand.Float);
                break;
            case OpcodeArgType.String:
                AppendQuoted(text, operand.String!);
                break;
            case OpcodeArgType.Resource:
                AppendResource(text, operand.String!);
                break;
            case OpcodeArgType.Reference:
                AppendReference(text, operand.Reference);
                break;
            case OpcodeArgType.Label:
                AppendOffset(text, operand.Int);
                break;
            case OpcodeArgType.TypeId:
            case OpcodeArgType.FilterId:
                text.Append(getTypePath(operand.Int));
                break;
            case OpcodeArgType.ValueType:
                text.Append(((DMValueType)operand.Int));
                break;
            default:
                text.Append(CultureInfo.InvariantCulture, $"{operand.Int}");
                break;
        }
    }

    private static void AppendOffset(StringBuilder text, int offset) {
        text.Append(CultureInfo.InvariantCulture, $"0x{offset:x}");
    }

    private static void AppendFloat(StringBuilder text, float value) {
        text.Append(CultureInfo.InvariantCulture, $"{value}");
    }

    private static void AppendReference(StringBuilder text, DMReference value) {
        text.Append(value.ToString());
    }

    private static void AppendQuoted(StringBuilder text, string value) {
        text.Append('"').Append(value).Append('"');
    }

    private static void AppendResource(StringBuilder text, string value) {
        text.Append('\'').Append(value).Append('\'');
    }
}
