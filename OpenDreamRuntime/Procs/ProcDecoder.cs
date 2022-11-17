using System.Runtime.CompilerServices;
using OpenDreamShared.Dream.Procs;

namespace OpenDreamRuntime.Procs;

public struct ProcDecoder {
    public readonly IReadOnlyList<string> Strings;
    public readonly byte[] Bytecode;
    public int Offset;

    public ProcDecoder(IReadOnlyList<string> strings, byte[] bytecode) {
        Strings = strings;
        Bytecode = bytecode;
        Offset = 0;
    }

    public bool Remaining => Offset < Bytecode.Length;

    public int ReadByte() {
        return Bytecode[Offset++];
    }

    public DreamProcOpcode ReadOpcode() {
        return (DreamProcOpcode) ReadByte();
    }

    public DreamProcOpcodeParameterType ReadParameterType() {
        return (DreamProcOpcodeParameterType) ReadByte();
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
        int stringID = ReadInt();
        return Strings[stringID];
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
            case DMReference.Type.SuperProc: return DMReference.SuperProc;
            case DMReference.Type.ListIndex: return DMReference.ListIndex;
            default: throw new Exception($"Invalid reference type {refType}");
        }
    }

    public ITuple DecodeInstruction() {
        var opcode = ReadOpcode();
        switch (opcode) {
            case DreamProcOpcode.FormatString:
                return (opcode, ReadString(), ReadInt());

            case DreamProcOpcode.PushString:
            case DreamProcOpcode.PushResource:
            case DreamProcOpcode.Initial:
            case DreamProcOpcode.IsSaved:
            case DreamProcOpcode.PushPath:
            case DreamProcOpcode.DebugSource:
            case DreamProcOpcode.DereferenceField:
            case DreamProcOpcode.DereferenceCall:
                return (opcode, ReadString());

            case DreamProcOpcode.Prompt:
                return (opcode, ReadValueType());

            case DreamProcOpcode.PushFloat:
                return (opcode, ReadFloat());

            case DreamProcOpcode.Call:
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
            case DreamProcOpcode.Enumerate:
            case DreamProcOpcode.OutputReference:
            case DreamProcOpcode.PushReferenceValue:
                return (opcode, ReadReference());

            case DreamProcOpcode.Input:
                return (opcode, ReadReference(), ReadReference());

            case DreamProcOpcode.CreateList:
            case DreamProcOpcode.CreateAssociativeList:
            case DreamProcOpcode.PickWeighted:
            case DreamProcOpcode.PickUnweighted:
            case DreamProcOpcode.Spawn:
            case DreamProcOpcode.BooleanOr:
            case DreamProcOpcode.BooleanAnd:
            case DreamProcOpcode.SwitchCase:
            case DreamProcOpcode.SwitchCaseRange:
            case DreamProcOpcode.Jump:
            case DreamProcOpcode.JumpIfFalse:
            case DreamProcOpcode.JumpIfTrue:
            case DreamProcOpcode.PushType:
            case DreamProcOpcode.DebugLine:
            case DreamProcOpcode.MassConcatenation:
            case DreamProcOpcode.JumpIfNullNoPop:
            case DreamProcOpcode.JumpIfTrueReferenceNoPop:
            case DreamProcOpcode.JumpIfFalseReferenceNoPop:
                return (opcode, ReadInt());

            case DreamProcOpcode.JumpIfNullDereference:
                return (opcode, ReadReference(), ReadInt());

            case DreamProcOpcode.PushArguments: {
                int argCount = ReadInt();
                int namedCount = ReadInt();
                string[] names = new string[argCount];

                for (int i = 0; i < argCount; i++) {
                    if (ReadParameterType() == DreamProcOpcodeParameterType.Named) {
                        names[i] = ReadString();
                    }
                }

                return (opcode, argCount, namedCount, names);
            }

            default:
                return ValueTuple.Create(opcode);
        }
    }

    public IEnumerable<(int Offset, ITuple Instruction)> Disassemble() {
        while (Remaining) {
            yield return (Offset, DecodeInstruction());
        }
    }
}
