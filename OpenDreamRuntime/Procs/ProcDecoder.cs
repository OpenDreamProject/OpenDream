using System.Runtime.CompilerServices;
using System.Text;
using OpenDreamShared.Common.Bytecode;
using OpenDreamShared.Common.DM;

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
            default: throw new Exception($"Invalid reference type {refType}");
        }
    }

    public ITuple DecodeInstruction() {
        var opcode = ReadOpcode();
        switch (opcode) {
            case DreamProcOpcode.FormatString:
                return (opcode, ReadString(), ReadInt());

            case DreamProcOpcode.PushStringFloat:
                return (opcode, ReadString(), ReadFloat());

            case DreamProcOpcode.PushString:
            case DreamProcOpcode.PushResource:
            case DreamProcOpcode.DereferenceField:
                return (opcode, ReadString());

            case DreamProcOpcode.DereferenceCall:
                return (opcode, ReadString(), (DMCallArgumentsType)ReadByte(), ReadInt());

            case DreamProcOpcode.Prompt:
                return (opcode, ReadValueType());

            case DreamProcOpcode.PushFloat:
            case DreamProcOpcode.ReturnFloat:
                return (opcode, ReadFloat());

            case DreamProcOpcode.SwitchOnFloat:
                return (opcode, ReadFloat(), ReadInt());

            case DreamProcOpcode.SwitchOnString:
                return (opcode, ReadString(), ReadInt());

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
                return (opcode, ReadReference());

            case DreamProcOpcode.Input:
                return (opcode, ReadReference(), ReadReference());

            case DreamProcOpcode.PushRefAndDereferenceField:
            case DreamProcOpcode.IndexRefWithString:
                return (opcode, ReadReference(), ReadString());

            case DreamProcOpcode.CallStatement:
            case DreamProcOpcode.CreateObject:
            case DreamProcOpcode.Gradient:
            case DreamProcOpcode.Rgb:
                return (opcode, (DMCallArgumentsType)ReadByte(), ReadInt());

            case DreamProcOpcode.Call:
                return (opcode, ReadReference(), (DMCallArgumentsType)ReadByte(), ReadInt());

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
                return (opcode, ReadInt());

            case DreamProcOpcode.JumpIfTrueReference:
            case DreamProcOpcode.JumpIfFalseReference:
            case DreamProcOpcode.JumpIfReferenceFalse:
                return (opcode, ReadReference(), ReadInt());

            case DreamProcOpcode.Try:
                return (opcode, ReadInt(), ReadReference());

            case DreamProcOpcode.Enumerate:
                return (opcode, ReadInt(), ReadReference(), ReadInt());

            case DreamProcOpcode.CreateFilteredListEnumerator:
            case DreamProcOpcode.EnumerateNoAssign:
                return (opcode, ReadInt(), ReadInt());

            case DreamProcOpcode.CreateListNRefs:
            case DreamProcOpcode.PushNRefs: {
                var count = ReadInt();
                var values = new DMReference[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadReference();
                }

                return (opcode, values);
            }

            case DreamProcOpcode.CreateListNStrings:
            case DreamProcOpcode.PushNStrings: {
                var count = ReadInt();
                var values = new string[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadString();
                }

                return (opcode, values);
            }

            case DreamProcOpcode.CreateListNFloats:
            case DreamProcOpcode.PushNFloats: {
                var count = ReadInt();
                var values = new float[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadFloat();
                }

                return (opcode, values);
            }

            case DreamProcOpcode.PushNOfStringFloats: {
                var count = ReadInt();
                var strings = new string[count];
                var floats = new float[count];

                for (int i = 0; i < count; i++) {
                    strings[i] = ReadString();
                    floats[i] = ReadFloat();
                }

                return (opcode, strings, floats);
            }

            case DreamProcOpcode.CreateListNResources:
            case DreamProcOpcode.PushNResources: {
                var count = ReadInt();
                var values = new string[count];

                for (int i = 0; i < count; i++) {
                    values[i] = ReadString();
                }

                return (opcode, values);
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

            case (DreamProcOpcode.PushResource, string str):
                text.Append('\'');
                text.Append(str);
                text.Append('\'');
                break;

            case (DreamProcOpcode.Spawn
                    or DreamProcOpcode.BooleanOr
                    or DreamProcOpcode.BooleanAnd
                    or DreamProcOpcode.SwitchCase
                    or DreamProcOpcode.SwitchCaseRange
                    or DreamProcOpcode.Jump
                    or DreamProcOpcode.JumpIfFalse, int jumpPosition):
                text.AppendFormat("0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.SwitchOnFloat, float value, int jumpPosition):
                text.Append(value);
                text.AppendFormat(" 0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.SwitchOnString, string value, int jumpPosition):
                text.Append('"');
                text.Append(value);
                text.Append("\" ");
                text.AppendFormat(" 0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.JumpIfFalseReference
                    or DreamProcOpcode.JumpIfTrueReference
                    or DreamProcOpcode.JumpIfReferenceFalse, DMReference reference, int jumpPosition):
                text.Append(reference.ToString());
                text.AppendFormat(" 0x{0:x}", jumpPosition);
                break;

            case (DreamProcOpcode.Enumerate, DMReference reference, int jumpPosition):
                text.Append(reference);
                text.Append(' ');
                text.Append(jumpPosition);
                break;

            case (DreamProcOpcode.PushType
                    or DreamProcOpcode.IsTypeDirect, int type):
                text.Append(getTypePath(type));
                break;

            case (DreamProcOpcode.CreateFilteredListEnumerator, int enumeratorId, int type):
                text.Append(enumeratorId);
                text.Append(' ');
                text.Append(getTypePath(type));
                break;

            case (DreamProcOpcode.CreateListNRefs
                    or DreamProcOpcode.PushNRefs, DMReference[] refs): {
                foreach (var reference in refs) {
                    text.Append(reference.ToString());
                    text.Append(' ');
                }

                break;
            }

            case (DreamProcOpcode.CreateListNStrings
                    or DreamProcOpcode.PushNStrings, string[] strings): {
                foreach (var value in strings) {
                    text.Append('"');
                    text.Append(value);
                    text.Append("\" ");
                }

                break;
            }

            case (DreamProcOpcode.CreateListNFloats
                    or DreamProcOpcode.PushNFloats, float[] floats): {
                foreach (var value in floats) {
                    text.Append(value);
                    text.Append(' ');
                }

                break;
            }

            case (DreamProcOpcode.CreateListNResources
                    or DreamProcOpcode.PushNResources, string[] resources): {
                foreach (var value in resources) {
                    text.Append('\'');
                    text.Append(value);
                    text.Append("' ");
                }

                break;
            }

            case (DreamProcOpcode.PushNOfStringFloats, string[] strings, float[] floats): {
                // The length of both arrays are equal
                for (var index = 0; index < strings.Length; index++) {
                    text.Append($"\"{strings[index]}\"");
                    text.Append(' ');
                    text.Append(floats[index]);
                    if(index + 1 < strings.Length) // Don't leave a trailing space
                        text.Append(' ');
                }

                break;
            }

            default:
                for (int i = 1; i < instruction.Length; ++i) {
                    var arg = instruction[i];

                    if (arg is string) {
                        text.Append('"');
                        text.Append(arg);
                        text.Append("\" ");
                    } else {
                        text.Append(instruction[i]);
                        text.Append(' ');
                    }
                }

                break;
        }

        return text.ToString();
    }

    public static int? GetJumpDestination(ITuple instruction) {
        switch (instruction) {
            case (DreamProcOpcode.Spawn
                    or DreamProcOpcode.BooleanOr
                    or DreamProcOpcode.BooleanAnd
                    or DreamProcOpcode.SwitchCase
                    or DreamProcOpcode.SwitchCaseRange
                    or DreamProcOpcode.Jump
                    or DreamProcOpcode.JumpIfFalse
                    or DreamProcOpcode.TryNoValue, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.JumpIfFalseReference
                    or DreamProcOpcode.JumpIfTrueReference
                    or DreamProcOpcode.JumpIfReferenceFalse, DMReference, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.SwitchOnFloat
                    or DreamProcOpcode.SwitchOnString, float or string, int jumpPosition):
                return jumpPosition;
            case (DreamProcOpcode.Try, int jumpPosition, DMReference):
                return jumpPosition;
            case (DreamProcOpcode.Enumerate, DMReference, int jumpPosition):
                return jumpPosition;
            default:
                return null;
        }
    }
}
