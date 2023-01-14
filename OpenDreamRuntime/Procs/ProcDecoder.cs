using System.Runtime.CompilerServices;
using System.Text;
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
            case DreamProcOpcode.OutputReference:
            case DreamProcOpcode.PushReferenceValue:
            case DreamProcOpcode.PopReference:
                return (opcode, ReadReference());

            case DreamProcOpcode.Input:
                return (opcode, ReadReference(), ReadReference());

            case DreamProcOpcode.CreateList:
            case DreamProcOpcode.CreateAssociativeList:
            case DreamProcOpcode.CreateFilteredListEnumerator:
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
            case DreamProcOpcode.PushProc:
            case DreamProcOpcode.PushProcStub:
            case DreamProcOpcode.PushVerbStub:
            case DreamProcOpcode.DebugLine:
            case DreamProcOpcode.MassConcatenation:
            case DreamProcOpcode.JumpIfNull:
            case DreamProcOpcode.JumpIfNullNoPop:
                return (opcode, ReadInt());

            case DreamProcOpcode.Enumerate:
            case DreamProcOpcode.JumpIfNullDereference:
            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfFalseReference:
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

            case (DreamProcOpcode.PushArguments, int argCount, int namedCount, string[] names):
                text.Append(argCount);
                for (int i = 0; i < argCount; i++) {
                    text.Append(' ');
                    text.Append(names[i] ?? "-");
                }

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
                    or DreamProcOpcode.JumpIfTrue, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.Enumerate, DMReference reference, int jumpPosition):
                return jumpPosition;
            default:
                return null;
        }
    }
}
