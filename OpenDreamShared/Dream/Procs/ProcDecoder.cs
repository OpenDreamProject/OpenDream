using System;
using System.Collections.Generic;

namespace OpenDreamShared.Dream.Procs;

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
            case DMReference.Type.Proc: return DMReference.CreateProc(ReadString());
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
}
