using DMCompiler.DM;

namespace DMCompiler.Bytecode;

public struct ProcDecoder(IReadOnlyList<string> strings, byte[] bytecode) {
    public readonly IReadOnlyList<string> Strings = strings;
    public readonly byte[] Bytecode = bytecode;
    public int Offset = 0;

    public bool Remaining => Offset < Bytecode.Length;

    public int ReadByte() {
        return Bytecode[Offset++];
    }

    public DreamProcOpcode ReadOpcode() {
        return (DreamProcOpcode) ReadByte();
    }

    public int ReadInt() {
        int value = BitConverter.ToInt32(Bytecode, Offset);
        Offset += 4;
        return value;
    }

    public DMValueType ReadValueType() {
        return (DMValueType) ReadInt();
    }

    public float ReadFloat() {
        float value = BitConverter.ToSingle(Bytecode, Offset);
        Offset += 4;
        return value;
    }

    public string ReadString() {
        int stringId = ReadInt();
        return Strings[stringId];
    }

    public DMReference ReadReference() {
        DMReference.Type refType = (DMReference.Type)ReadByte();

        switch (refType) {
            case DMReference.Type.Argument: return DMReference.CreateArgument(ReadByte());
            case DMReference.Type.Local: return DMReference.CreateLocal(ReadByte());
            case DMReference.Type.Global: return DMReference.CreateGlobal(ReadInt());
            case DMReference.Type.GlobalProc: return DMReference.CreateGlobalProc(ReadInt());
            case DMReference.Type.Field: return DMReference.CreateField(ReadString());
            case DMReference.Type.SrcField: return DMReference.CreateSrcField(ReadString());
            case DMReference.Type.SrcProc: return DMReference.CreateSrcProc(ReadString());
            case DMReference.Type.Src: return DMReference.Src;
            case DMReference.Type.Self: return DMReference.Self;
            case DMReference.Type.Usr: return DMReference.Usr;
            case DMReference.Type.Args: return DMReference.Args;
            case DMReference.Type.World: return DMReference.World;
            case DMReference.Type.SuperProc: return DMReference.SuperProc;
            case DMReference.Type.ListIndex: return DMReference.ListIndex;
            case DMReference.Type.Caller: return DMReference.Caller;
            case DMReference.Type.Callee: return DMReference.Callee;
            case DMReference.Type.NoRef: return new DMReference { RefType = DMReference.Type.NoRef };
            default: throw new Exception($"Invalid reference type {refType}");
        }
    }

    public ProcInstruction DecodeInstruction() {
        int offset = Offset;
        DreamProcOpcode opcode = ReadOpcode();
        OpcodeMetadata metadata = OpcodeMetadataCache.GetMetadata(opcode);

        return metadata.VariableArgs
            ? DecodeVariableInstruction(offset, opcode, metadata)
            : DecodeFixedInstruction(offset, opcode, metadata);
    }

    public IEnumerable<(int Offset, ProcInstruction Instruction)> Disassemble() {
        while (Remaining) {
            yield return (Offset, DecodeInstruction());
        }
    }

    public static string Format(ProcInstruction instruction, Func<int, string> getTypePath) {
        return instruction.Format(getTypePath);
    }

    public static int? GetJumpDestination(ProcInstruction instruction) {
        return instruction.GetJumpDestination();
    }

    private ProcInstruction DecodeFixedInstruction(int offset, DreamProcOpcode opcode, OpcodeMetadata metadata) {
        switch (metadata.RequiredArgs.Length) {
            case 0:
                return new ProcInstruction(offset, Offset, opcode);
            case 1: {
                ProcOperand operand0 = ReadOperand(metadata.RequiredArgs[0]);
                return new ProcInstruction(offset, Offset, opcode, 1, operand0: operand0);
            }
            case 2: {
                ProcOperand operand0 = ReadOperand(metadata.RequiredArgs[0]);
                ProcOperand operand1 = ReadOperand(metadata.RequiredArgs[1]);
                return new ProcInstruction(offset, Offset, opcode, 2, operand0: operand0, operand1: operand1);
            }
            case 3: {
                ProcOperand operand0 = ReadOperand(metadata.RequiredArgs[0]);
                ProcOperand operand1 = ReadOperand(metadata.RequiredArgs[1]);
                ProcOperand operand2 = ReadOperand(metadata.RequiredArgs[2]);
                return new ProcInstruction(offset, Offset, opcode, 3, operand0: operand0, operand1: operand1,
                    operand2: operand2);
            }
            case 4: {
                ProcOperand operand0 = ReadOperand(metadata.RequiredArgs[0]);
                ProcOperand operand1 = ReadOperand(metadata.RequiredArgs[1]);
                ProcOperand operand2 = ReadOperand(metadata.RequiredArgs[2]);
                ProcOperand operand3 = ReadOperand(metadata.RequiredArgs[3]);
                return new ProcInstruction(offset, Offset, opcode, 4, operand0: operand0, operand1: operand1,
                    operand2: operand2, operand3: operand3);
            }
            default:
                throw new InvalidOperationException(
                    $"Opcode {opcode} has too many operands (args length: {metadata.RequiredArgs.Length}) for ProcInstruction {nameof(ProcInstruction)}");
        }
    }

    private ProcInstruction DecodeVariableInstruction(int offset, DreamProcOpcode opcode, OpcodeMetadata metadata) {
        int count = ReadInt();

        switch (metadata.RepeatedArgs.Length) {
            case 1: {
                ProcOperand operand0 = ReadRepeatedOperand(metadata.RepeatedArgs[0], count);
                return new ProcInstruction(offset, Offset, opcode, 1, metadata.OperandShape, operand0);
            }
            case 2: {
                (ProcOperand operand0, ProcOperand operand1) =
                    ReadRepeatedOperandPair(metadata.RepeatedArgs[0], metadata.RepeatedArgs[1], count);
                return new ProcInstruction(offset, Offset, opcode, 2, metadata.OperandShape, operand0,
                    operand1);
            }
            default:
                throw new InvalidOperationException(
                    $"Variable arg opcode {opcode} has an unsupported repeated operand shape (args length: {metadata.RepeatedArgs.Length})");
        }
    }

    private ProcOperand ReadOperand(OpcodeArgType argType) {
        switch (argType) {
            case OpcodeArgType.ArgType:
                return ProcOperand.FromInt(ReadByte());
            case OpcodeArgType.Float:
                return ProcOperand.FromFloat(ReadFloat());
            case OpcodeArgType.String:
            case OpcodeArgType.Resource:
                return ProcOperand.FromString(ReadString());
            case OpcodeArgType.Reference:
                return ProcOperand.FromReference(ReadReference());
            case OpcodeArgType.ValueType:
                return ProcOperand.FromInt((int)ReadValueType());
            default:
                return ProcOperand.FromInt(ReadInt());
        }
    }

    private ProcOperand ReadRepeatedOperand(OpcodeArgType argType, int count) {
        switch (argType) {
            case OpcodeArgType.Float: {
                float[] values = new float[count];
                for (int i = 0; i < count; i++) {
                    values[i] = ReadFloat();
                }

                return ProcOperand.FromFloats(values);
            }
            case OpcodeArgType.String:
            case OpcodeArgType.Resource: {
                string[] values = new string[count];
                for (int i = 0; i < count; i++) {
                    values[i] = ReadString();
                }

                return ProcOperand.FromStrings(values);
            }
            case OpcodeArgType.Reference: {
                DMReference[] values = new DMReference[count];
                for (int i = 0; i < count; i++) {
                    values[i] = ReadReference();
                }

                return ProcOperand.FromReferences(values);
            }
            default:
                throw new InvalidOperationException($"Unsupported repeated operand type {argType} (count: {count})");
        }
    }

    private (ProcOperand First, ProcOperand Second) ReadRepeatedOperandPair(OpcodeArgType firstArgType,
        OpcodeArgType secondArgType, int count) {
        switch ((firstArgType, secondArgType)) {
            case (OpcodeArgType.String, OpcodeArgType.Float): {
                string[] strings = new string[count];
                float[] floats = new float[count];

                for (int i = 0; i < count; i++) {
                    strings[i] = ReadString();
                    floats[i] = ReadFloat();
                }

                return (ProcOperand.FromStrings(strings), ProcOperand.FromFloats(floats));
            }
            case (OpcodeArgType.Float, OpcodeArgType.Reference): {
                float[] floats = new float[count];
                DMReference[] references = new DMReference[count];

                for (int i = 0; i < count; i++) {
                    floats[i] = ReadFloat();
                    references[i] = ReadReference();
                }

                return (ProcOperand.FromFloats(floats), ProcOperand.FromReferences(references));
            }
            default:
                throw new InvalidOperationException(
                    $"Unsupported repeated operand pair {firstArgType}, {secondArgType}");
        }
    }
}
