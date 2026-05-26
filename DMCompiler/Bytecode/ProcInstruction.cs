using System.Globalization;
using System.Text;
using DMCompiler.DM;

namespace DMCompiler.Bytecode;

public enum ProcOperandKind : byte {
    None,
    Int,
    Float,
    String,
    Reference,
    CallArgumentsType,
    References,
    Floats,
    Strings
}

/// <summary>
/// A typed bytecode operand. Array operands are used only by compact variable-argument opcodes
/// TODO: When .NET 11 is out, revisit this with C# 15 unions
/// </summary>
public readonly struct ProcOperand {
    public readonly ProcOperandKind Kind;
    public readonly int Int;
    public readonly float Float;
    public readonly string? String;
    public readonly DMReference Reference;
    public readonly DMCallArgumentsType CallArgumentsType;
    public readonly DMReference[]? References;
    public readonly float[]? Floats;
    public readonly string[]? Strings;

    private ProcOperand(ProcOperandKind kind, int intValue = default, float floatValue = default,
        string? stringValue = default, DMReference reference = default,
        DMCallArgumentsType callArgumentsType = default, DMReference[]? references = default,
        float[]? floats = default, string[]? strings = default) {
        Kind = kind;
        Int = intValue;
        Float = floatValue;
        String = stringValue;
        Reference = reference;
        CallArgumentsType = callArgumentsType;
        References = references;
        Floats = floats;
        Strings = strings;
    }

    public static ProcOperand FromInt(int value) {
        return new ProcOperand(ProcOperandKind.Int, intValue: value);
    }

    public static ProcOperand FromFloat(float value) {
        return new ProcOperand(ProcOperandKind.Float, floatValue: value);
    }

    public static ProcOperand FromString(string value) {
        return new ProcOperand(ProcOperandKind.String, stringValue: value);
    }

    public static ProcOperand FromReference(DMReference value) {
        return new ProcOperand(ProcOperandKind.Reference, reference: value);
    }

    public static ProcOperand FromCallArgumentsType(DMCallArgumentsType value) {
        return new ProcOperand(ProcOperandKind.CallArgumentsType, callArgumentsType: value);
    }

    public static ProcOperand FromReferences(DMReference[] values) {
        return new ProcOperand(ProcOperandKind.References, references: values);
    }

    public static ProcOperand FromFloats(float[] values) {
        return new ProcOperand(ProcOperandKind.Floats, floats: values);
    }

    public static ProcOperand FromStrings(string[] values) {
        return new ProcOperand(ProcOperandKind.Strings, strings: values);
    }

    public override string ToString() {
        return Kind switch {
            ProcOperandKind.Int => Int.ToString(),
            ProcOperandKind.Float => Float.ToString(CultureInfo.InvariantCulture),
            ProcOperandKind.String => String ?? string.Empty,
            ProcOperandKind.Reference => Reference.ToString(),
            ProcOperandKind.CallArgumentsType => CallArgumentsType.ToString(),
            ProcOperandKind.References => References is null ? string.Empty : string.Join(' ', References),
            ProcOperandKind.Floats => Floats is null ? string.Empty : string.Join(' ', Floats),
            ProcOperandKind.Strings => Strings is null ? string.Empty : string.Join(' ', Strings),
            _ => string.Empty
        };
    }
}

/// <summary>
/// A decoded instruction from a proc bytecode stream
/// </summary>
public readonly struct ProcInstruction(int offset, int endOffset, DreamProcOpcode opcode) {
    public readonly int Offset = offset;

    // ReSharper disable once UnusedMember.Global
    public readonly int EndOffset = endOffset; // TODO: CFG exporter will use this
    public readonly DreamProcOpcode Opcode = opcode;
    public readonly int OperandCount = 0;
    private readonly ProcOperand _operand0;
    private readonly ProcOperand _operand1;
    private readonly ProcOperand _operand2;
    private readonly ProcOperand _operand3;

    public ProcInstruction(int offset, int endOffset, DreamProcOpcode opcode, ProcOperand operand0) : this(offset,
        endOffset, opcode) {
        OperandCount = 1;
        _operand0 = operand0;
    }

    public ProcInstruction(int offset, int endOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1) : this(offset, endOffset, opcode, operand0) {
        OperandCount = 2;
        _operand1 = operand1;
    }

    public ProcInstruction(int offset, int endOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1, ProcOperand operand2) : this(offset, endOffset, opcode, operand0, operand1) {
        OperandCount = 3;
        _operand2 = operand2;
    }

    public ProcInstruction(int offset, int endOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1, ProcOperand operand2, ProcOperand operand3) : this(offset, endOffset, opcode, operand0,
        operand1, operand2) {
        OperandCount = 4;
        _operand3 = operand3;
    }

    public ProcOperand GetOperand(int index) {
        if ((uint)index >= (uint)OperandCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return index switch {
            0 => _operand0,
            1 => _operand1,
            2 => _operand2,
            3 => _operand3,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
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

    public DMCallArgumentsType GetCallArgumentsType(int index) {
        return GetOperand(index).CallArgumentsType;
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
        StringBuilder text = new();
        text.Append(Opcode.ToString());
        text.Append(' ');

        switch (Opcode) {
            case DreamProcOpcode.FormatString:
                text.Append(GetInt(1));
                text.Append(' ');
                AppendQuoted(text, GetString(0));
                break;

            case DreamProcOpcode.PushResource:
                AppendResource(text, GetString(0));
                break;

            case DreamProcOpcode.Spawn:
            case DreamProcOpcode.BooleanOr:
            case DreamProcOpcode.BooleanAnd:
            case DreamProcOpcode.SwitchCase:
            case DreamProcOpcode.SwitchCaseRange:
            case DreamProcOpcode.Jump:
            case DreamProcOpcode.JumpIfFalse:
            case DreamProcOpcode.JumpIfNull:
            case DreamProcOpcode.JumpIfNullNoPop:
            case DreamProcOpcode.TryNoValue:
                AppendOffset(text, GetInt(0));
                break;

            case DreamProcOpcode.SwitchOnFloat:
                text.Append(GetFloat(0));
                text.Append(' ');
                AppendOffset(text, GetInt(1));
                break;

            case DreamProcOpcode.SwitchOnString:
                AppendQuoted(text, GetString(0));
                text.Append(' ');
                AppendOffset(text, GetInt(1));
                break;

            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfReferenceFalse:
                text.Append(GetReference(0).ToString());
                text.Append(' ');
                AppendOffset(text, GetInt(1));
                break;

            case DreamProcOpcode.Try:
                AppendOffset(text, GetInt(0));
                text.Append(' ');
                text.Append(GetReference(1).ToString());
                break;

            case DreamProcOpcode.Enumerate:
                text.Append(GetInt(0));
                text.Append(' ');
                text.Append(GetReference(1).ToString());
                text.Append(' ');
                AppendOffset(text, GetInt(2));
                break;

            case DreamProcOpcode.EnumerateAssoc:
                text.Append(GetInt(0));
                text.Append(' ');
                text.Append(GetReference(1).ToString());
                text.Append(' ');
                text.Append(GetReference(2).ToString());
                text.Append(' ');
                AppendOffset(text, GetInt(3));
                break;

            case DreamProcOpcode.EnumerateNoAssign:
                text.Append(GetInt(0));
                text.Append(' ');
                AppendOffset(text, GetInt(1));
                break;

            case DreamProcOpcode.PushType:
            case DreamProcOpcode.IsTypeDirect:
                text.Append(getTypePath(GetInt(0)));
                break;

            case DreamProcOpcode.Prompt:
                text.Append((((DMValueType)GetInt(0))).ToString());
                break;

            case DreamProcOpcode.CreateFilteredListEnumerator:
                text.Append(GetInt(0));
                text.Append(' ');
                text.Append(getTypePath(GetInt(1)));
                break;

            case DreamProcOpcode.CreateListNRefs:
            case DreamProcOpcode.PushNRefs:
                AppendMany(text, GetReferences(0));
                break;

            case DreamProcOpcode.CreateListNStrings:
            case DreamProcOpcode.PushNStrings:
                AppendManyQuoted(text, GetStrings(0));
                break;

            case DreamProcOpcode.CreateListNFloats:
            case DreamProcOpcode.PushNFloats:
                AppendMany(text, GetFloats(0));
                break;

            case DreamProcOpcode.CreateListNResources:
            case DreamProcOpcode.PushNResources:
                AppendManyResources(text, GetStrings(0));
                break;

            case DreamProcOpcode.PushNOfStringFloats: {
                string[] strings = GetStrings(0);
                float[] floats = GetFloats(1);
                for (int i = 0; i < strings.Length; i++) {
                    AppendQuoted(text, strings[i]);
                    text.Append(' ');
                    text.Append(floats[i]);
                    if (i + 1 < strings.Length)
                        text.Append(' ');
                }

                break;
            }

            case DreamProcOpcode.PushFloatAssign:
                text.Append(GetFloat(0));
                text.Append(' ');
                text.Append(GetReference(1).ToString());
                break;

            case DreamProcOpcode.NPushFloatAssign: {
                float[] floats = GetFloats(0);
                DMReference[] refs = GetReferences(1);
                for (int i = 0; i < refs.Length; i++) {
                    text.Append(refs[i].ToString());
                    text.Append('=');
                    text.Append(floats[i]);
                    if (i + 1 < refs.Length)
                        text.Append(' ');
                }

                break;
            }

            default:
                AppendDefaultOperands(text);
                break;
        }

        return text.ToString();
    }

    private void AppendDefaultOperands(StringBuilder text) {
        for (int i = 0; i < OperandCount; i++) {
            ProcOperand operand = GetOperand(i);
            if (operand.Kind == ProcOperandKind.String) {
                AppendQuoted(text, operand.String!);
            } else {
                text.Append(operand.ToString());
            }

            text.Append(' ');
        }
    }

    private void AppendOffset(StringBuilder text, int offset) {
        text.AppendFormat("0x{0:x}", offset);
    }

    private void AppendQuoted(StringBuilder text, string value) {
        text.Append('"');
        text.Append(value);
        text.Append('"');
    }

    private void AppendResource(StringBuilder text, string value) {
        text.Append('\'');
        text.Append(value);
        text.Append('\'');
    }

    private void AppendMany<T>(StringBuilder text, IReadOnlyList<T> values) {
        for (int i = 0; i < values.Count; i++) {
            text.Append(values[i]);
            if (i + 1 < values.Count)
                text.Append(' ');
        }
    }

    private void AppendManyQuoted(StringBuilder text, IReadOnlyList<string> values) {
        for (int i = 0; i < values.Count; i++) {
            AppendQuoted(text, values[i]);
            if (i + 1 < values.Count)
                text.Append(' ');
        }
    }

    private void AppendManyResources(StringBuilder text, IReadOnlyList<string> values) {
        for (int i = 0; i < values.Count; i++) {
            AppendResource(text, values[i]);
            if (i + 1 < values.Count)
                text.Append(' ');
        }
    }
}
