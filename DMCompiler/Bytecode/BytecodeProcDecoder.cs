namespace DMCompiler.Bytecode;

/// <summary>
/// Decodes OpenDream proc bytecode into typed instruction records
/// </summary>
public sealed class BytecodeProcDecoder(IReadOnlyList<string> strings, byte[] bytecode) {
    public readonly IReadOnlyList<string> Strings = strings;
    public readonly byte[] Bytecode = bytecode;
    public int Offset = 0;

    public bool Remaining => Offset < Bytecode.Length;

    public int ReadByte() {
        return Bytecode[Offset++];
    }

    public DreamProcOpcode ReadOpcode() {
        return (DreamProcOpcode)ReadByte();
    }

    public int ReadInt() {
        int value = BitConverter.ToInt32(Bytecode, Offset);
        Offset += 4;
        return value;
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

    public int? GetBranchTarget(ProcInstruction instruction) {
        switch (instruction.Opcode) {
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
                return instruction.GetInt(0);
            case DreamProcOpcode.SwitchOnFloat:
            case DreamProcOpcode.SwitchOnString:
                return instruction.GetInt(1);
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfReferenceFalse:
                return instruction.GetInt(1);
            case DreamProcOpcode.Try:
                return instruction.GetInt(0);
            case DreamProcOpcode.Enumerate:
                return instruction.GetInt(2);
            case DreamProcOpcode.EnumerateAssoc:
                return instruction.GetInt(3);
            case DreamProcOpcode.EnumerateNoAssign:
                return instruction.GetInt(1);
            default:
                return null;
        }
    }

    public ProcInstruction DecodeInstruction() {
        int startOffset = Offset;
        DreamProcOpcode opcode = ReadOpcode();

        switch (opcode) {
            case DreamProcOpcode.FormatString:
                return Instruction(startOffset, opcode, ProcOperand.FromString(ReadString()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.PushStringFloat:
                return Instruction(startOffset, opcode, ProcOperand.FromString(ReadString()), ProcOperand.FromFloat(ReadFloat()));
            case DreamProcOpcode.PushFloatAssign:
                return Instruction(startOffset, opcode, ProcOperand.FromFloat(ReadFloat()), ProcOperand.FromReference(ReadReference()));
            case DreamProcOpcode.NPushFloatAssign: {
                var count = ReadInt();
                var floats = new float[count];
                var refs = new DMReference[count];

                for (int i = 0; i < count; i++) {
                    floats[i] = ReadFloat();
                    refs[i] = ReadReference();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromFloats(floats), ProcOperand.FromReferences(refs));
            }
            case DreamProcOpcode.PushString:
            case DreamProcOpcode.PushResource:
            case DreamProcOpcode.DereferenceField:
                return Instruction(startOffset, opcode, ProcOperand.FromString(ReadString()));

            case DreamProcOpcode.DereferenceCall:
                return Instruction(startOffset, opcode, ProcOperand.FromString(ReadString()),
                    ProcOperand.FromCallArgumentsType((DMCallArgumentsType)ReadByte()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.Prompt:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.PushFloat:
            case DreamProcOpcode.ReturnFloat:
                return Instruction(startOffset, opcode, ProcOperand.FromFloat(ReadFloat()));

            case DreamProcOpcode.SwitchOnFloat:
                return Instruction(startOffset, opcode, ProcOperand.FromFloat(ReadFloat()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.SwitchOnString:
                return Instruction(startOffset, opcode, ProcOperand.FromString(ReadString()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.Assign:
            case DreamProcOpcode.Append:
            case DreamProcOpcode.Remove:
            case DreamProcOpcode.Combine:
            case DreamProcOpcode.Increment:
            case DreamProcOpcode.Decrement:
            case DreamProcOpcode.Mask:
            case DreamProcOpcode.MultiplyReference:
            case DreamProcOpcode.DivideReference:
            case DreamProcOpcode.BitXorReference:
            case DreamProcOpcode.ModulusReference:
            case DreamProcOpcode.BitShiftLeftReference:
            case DreamProcOpcode.BitShiftRightReference:
            case DreamProcOpcode.OutputReference:
            case DreamProcOpcode.PushReferenceValue:
            case DreamProcOpcode.PopReference:
            case DreamProcOpcode.AppendNoPush:
            case DreamProcOpcode.NullRef:
            case DreamProcOpcode.AssignNoPush:
            case DreamProcOpcode.ReturnReferenceValue:
                return Instruction(startOffset, opcode, ProcOperand.FromReference(ReadReference()));

            case DreamProcOpcode.Input:
                return Instruction(startOffset, opcode, ProcOperand.FromReference(ReadReference()),
                    ProcOperand.FromReference(ReadReference()));

            case DreamProcOpcode.PushRefAndDereferenceField:
            case DreamProcOpcode.IndexRefWithString:
                return Instruction(startOffset, opcode, ProcOperand.FromReference(ReadReference()),
                    ProcOperand.FromString(ReadString()));

            case DreamProcOpcode.CallStatement:
            case DreamProcOpcode.CreateObject:
            case DreamProcOpcode.Gradient:
            case DreamProcOpcode.Rgb:
            case DreamProcOpcode.Animate:
                return Instruction(startOffset, opcode, ProcOperand.FromCallArgumentsType((DMCallArgumentsType)ReadByte()),
                    ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.Call:
                return Instruction(startOffset, opcode, ProcOperand.FromReference(ReadReference()),
                    ProcOperand.FromCallArgumentsType((DMCallArgumentsType)ReadByte()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.CreateList:
            case DreamProcOpcode.CreateAssociativeList:
            case DreamProcOpcode.CreateStrictAssociativeList:
            case DreamProcOpcode.PickWeighted:
            case DreamProcOpcode.PickUnweighted:
            case DreamProcOpcode.Spawn:
            case DreamProcOpcode.BooleanOr:
            case DreamProcOpcode.BooleanAnd:
            case DreamProcOpcode.SwitchCase:
            case DreamProcOpcode.SwitchCaseRange:
            case DreamProcOpcode.Jump:
            case DreamProcOpcode.JumpIfFalse:
            case DreamProcOpcode.PushType:
            case DreamProcOpcode.PushProc:
            case DreamProcOpcode.MassConcatenation:
            case DreamProcOpcode.JumpIfNull:
            case DreamProcOpcode.JumpIfNullNoPop:
            case DreamProcOpcode.TryNoValue:
            case DreamProcOpcode.CreateListEnumerator:
            case DreamProcOpcode.CreateRangeEnumerator:
            case DreamProcOpcode.CreateTypeEnumerator:
            case DreamProcOpcode.DestroyEnumerator:
            case DreamProcOpcode.IsTypeDirect:
            case DreamProcOpcode.CreateMultidimensionalList:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfReferenceFalse:
                return Instruction(startOffset, opcode, ProcOperand.FromReference(ReadReference()),
                    ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.Try:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()),
                    ProcOperand.FromReference(ReadReference()));

            case DreamProcOpcode.Enumerate:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()),
                    ProcOperand.FromReference(ReadReference()), ProcOperand.FromInt(ReadInt()));
            case DreamProcOpcode.EnumerateAssoc:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()),
                    ProcOperand.FromReference(ReadReference()), ProcOperand.FromReference(ReadReference()),
                    ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.CreateFilteredListEnumerator:
            case DreamProcOpcode.EnumerateNoAssign:
                return Instruction(startOffset, opcode, ProcOperand.FromInt(ReadInt()), ProcOperand.FromInt(ReadInt()));

            case DreamProcOpcode.CreateListNRefs:
            case DreamProcOpcode.PushNRefs: {
                var count = ReadInt();
                var values = new DMReference[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadReference();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromReferences(values));
            }

            case DreamProcOpcode.CreateListNStrings:
            case DreamProcOpcode.PushNStrings: {
                var count = ReadInt();
                var values = new string[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadString();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromStrings(values));
            }

            case DreamProcOpcode.CreateListNFloats:
            case DreamProcOpcode.PushNFloats: {
                var count = ReadInt();
                var values = new float[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadFloat();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromFloats(values));
            }

            case DreamProcOpcode.PushNOfStringFloats: {
                var count = ReadInt();
                var strings = new string[count];
                var floats = new float[count];

                for (int i = 0; i < count; i++) {
                    strings[i] = ReadString();
                    floats[i] = ReadFloat();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromStrings(strings), ProcOperand.FromFloats(floats));
            }

            case DreamProcOpcode.CreateListNResources:
            case DreamProcOpcode.PushNResources: {
                var count = ReadInt();
                var values = new string[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadString();
                }

                return Instruction(startOffset, opcode, ProcOperand.FromStrings(values));
            }

            default:
                return Instruction(startOffset, opcode);
        }
    }

    private ProcInstruction Instruction(int startOffset, DreamProcOpcode opcode) {
        return new ProcInstruction(startOffset, Offset, opcode);
    }

    private ProcInstruction Instruction(int startOffset, DreamProcOpcode opcode, ProcOperand operand0) {
        return new ProcInstruction(startOffset, Offset, opcode, operand0);
    }

    private ProcInstruction Instruction(int startOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1) {
        return new ProcInstruction(startOffset, Offset, opcode, operand0, operand1);
    }

    private ProcInstruction Instruction(int startOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1, ProcOperand operand2) {
        return new ProcInstruction(startOffset, Offset, opcode, operand0, operand1, operand2);
    }

    private ProcInstruction Instruction(int startOffset, DreamProcOpcode opcode, ProcOperand operand0,
        ProcOperand operand1, ProcOperand operand2, ProcOperand operand3) {
        return new ProcInstruction(startOffset, Offset, opcode, operand0, operand1, operand2, operand3);
    }

    public IEnumerable<(int Offset, ProcInstruction Instruction)> Disassemble() {
        while (Remaining) {
            ProcInstruction instruction = DecodeInstruction();
            yield return (instruction.Offset, instruction);
        }
    }
}
