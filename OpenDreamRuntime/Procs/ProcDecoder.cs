using System.Runtime.CompilerServices;
using System.Text;
using DMCompiler.Bytecode;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Procs;

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

    public DreamValueType ReadValueType() {
        return (DreamValueType) ReadInt();
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
            case DreamProcOpcode.DereferenceField:
                return (opcode, ReadString());

            case DreamProcOpcode.DereferenceCall:
                return (opcode, ReadString(), (DMCallArgumentsType)ReadByte(), ReadInt());

            case DreamProcOpcode.Prompt:
                return (opcode, ReadValueType());

            case DreamProcOpcode.PushFloat:
                return (opcode, ReadFloat());

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
                return (opcode, ReadReference());

            case DreamProcOpcode.Input:
                return (opcode, ReadReference(), ReadReference());

            case DreamProcOpcode.CallStatement:
            case DreamProcOpcode.CreateObject:
            case DreamProcOpcode.Gradient:
                return (opcode, (DMCallArgumentsType)ReadByte(), ReadInt());

            case DreamProcOpcode.Call:
                return (opcode, ReadReference(), (DMCallArgumentsType)ReadByte(), ReadInt());

            case DreamProcOpcode.EnumerateNoAssign:
            case DreamProcOpcode.CreateList:
            case DreamProcOpcode.CreateAssociativeList:
            case DreamProcOpcode.CreateFilteredListEnumerator:
            case DreamProcOpcode.PickWeighted:
            case DreamProcOpcode.PickUnweighted:
            case DreamProcOpcode.Spawn:
            case DreamProcOpcode.Sleep:
            case DreamProcOpcode.BooleanOr:
            case DreamProcOpcode.BooleanAnd:
            case DreamProcOpcode.SwitchCase:
            case DreamProcOpcode.SwitchCaseRange:
            case DreamProcOpcode.Jump:
            case DreamProcOpcode.JumpIfFalse:
            case DreamProcOpcode.JumpIfTrue:
            case DreamProcOpcode.PushType:
            case DreamProcOpcode.PushProc:
            case DreamProcOpcode.MassConcatenation:
            case DreamProcOpcode.JumpIfNull:
            case DreamProcOpcode.JumpIfNullNoPop:
            case DreamProcOpcode.TryNoValue:
                return (opcode, ReadInt());

            case DreamProcOpcode.JumpIfNullDereference:
            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.Enumerate:
                return (opcode, ReadReference(), ReadInt());

            case DreamProcOpcode.Try:
                return (opcode, ReadInt(), ReadReference());

            default:
                return ValueTuple.Create(opcode);
        }
    }

    public IEnumerable<(int Offset, ITuple Instruction)> Disassemble() {
        while (Remaining) {
            yield return (Offset, DecodeInstruction());
        }
    }

    public static string Format(ITuple instruction, Func<int, string> getTypePath) {
        StringBuilder text = new StringBuilder();
        text.Append(instruction[0]);
        text.Append(' ');
        switch (instruction) {
            case (DreamProcOpcode.FormatString, string str, int numReplacements):
                text.Append(numReplacements);
                text.Append(' ');
                text.Append('"');
                text.Append(str);
                text.Append('"');
                break;

            case (DreamProcOpcode.PushString, string str):
                text.Append('"');
                text.Append(str);
                text.Append('"');
                break;

            case (DreamProcOpcode.PushResource, string str):
                text.Append('\'');
                text.Append(str);
                text.Append('\'');
                break;

            case (DreamProcOpcode.JumpIfNullDereference, DMReference reference, int jumpPosition):
                text.Append(reference);
                text.AppendFormat(" 0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.Spawn
                    or DreamProcOpcode.BooleanOr
                    or DreamProcOpcode.BooleanAnd
                    or DreamProcOpcode.SwitchCase
                    or DreamProcOpcode.SwitchCaseRange
                    or DreamProcOpcode.Jump
                    or DreamProcOpcode.JumpIfFalse
                    or DreamProcOpcode.JumpIfTrue, int jumpPosition):
                text.AppendFormat("0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.Enumerate, DMReference reference, int jumpPosition):
                text.Append(reference);
                text.Append(' ');
                text.Append(jumpPosition);
                break;

            case (DreamProcOpcode.PushType, int type):
                text.Append(getTypePath(type));
                break;

            default:
                for (int i = 1; i < instruction.Length; ++i) {
                    text.Append(instruction[i]);
                    text.Append(' ');
                }
                break;
        }
        return text.ToString();
    }

    public static int? GetJumpDestination(ITuple instruction) {
        switch (instruction) {
            case (DreamProcOpcode.JumpIfNullDereference, DMReference reference, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.Spawn
                    or DreamProcOpcode.BooleanOr
                    or DreamProcOpcode.BooleanAnd
                    or DreamProcOpcode.SwitchCase
                    or DreamProcOpcode.SwitchCaseRange
                    or DreamProcOpcode.Jump
                    or DreamProcOpcode.JumpIfFalse
                    or DreamProcOpcode.JumpIfTrue
                    or DreamProcOpcode.TryNoValue, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.Try, int jumpPosition, DMReference dmReference):
                return jumpPosition;
            case (DreamProcOpcode.Enumerate, DMReference reference, int jumpPosition):
                return jumpPosition;
            default:
                return null;
        }
    }
}
