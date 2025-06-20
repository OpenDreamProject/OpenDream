using System.Runtime.CompilerServices;

namespace DMCompiler.Bytecode;

public struct DMReference {
    public static readonly DMReference Src = new() { RefType = Type.Src };
    public static readonly DMReference Self = new() { RefType = Type.Self };
    public static readonly DMReference Usr = new() { RefType = Type.Usr };
    public static readonly DMReference Args = new() { RefType = Type.Args };
    public static readonly DMReference World = new() { RefType = Type.World };
    public static readonly DMReference SuperProc = new() { RefType = Type.SuperProc };
    public static readonly DMReference ListIndex = new() { RefType = Type.ListIndex };
    public static readonly DMReference Callee = new() { RefType = Type.Callee };
    public static readonly DMReference Caller = new() { RefType = Type.Caller };
    public static readonly DMReference Invalid = new() { RefType = Type.Invalid };

    public enum Type : byte {
        Src,
        Self,
        Usr,
        Args,
        World,
        SuperProc,
        ListIndex,
        Argument,
        Local,
        Global,
        GlobalProc,
        Field,
        SrcField,
        SrcProc,
        Callee,
        Caller,

        /// <summary>
        /// Something went wrong in the creation of this DMReference, and so this reference is not valid
        /// </summary>
        /// <remarks>
        /// Be sure to emit a compiler error before creating
        /// </remarks>
        Invalid
    }

    public Type RefType;

    //Argument, Local, Global, GlobalProc
    public int Index;

    //Field, SrcField, Proc, SrcProc
    public string Name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateArgument(int argId) {
        if (argId > 255) throw new Exception("Argument id is greater than the maximum of 255");

        return new DMReference { RefType = Type.Argument, Index = (byte)argId };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateLocal(int local) {
        if (local > 255) throw new Exception("Local variable id is greater than the maximum of 255");

        return new DMReference { RefType = Type.Local, Index = (byte)local };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateGlobal(int global) {
        return new DMReference { RefType = Type.Global, Index = global };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateField(string fieldName) {
        return new DMReference { RefType = Type.Field, Name = fieldName };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateSrcField(string fieldName) {
        return new DMReference { RefType = Type.SrcField, Name = fieldName };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateGlobalProc(int procId) {
        return new DMReference { RefType = Type.GlobalProc, Index = procId };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DMReference CreateSrcProc(string procName) {
        return new DMReference { RefType = Type.SrcProc, Name = procName };
    }

    public override string ToString() {
        switch (RefType) {
            case Type.Local:
            case Type.Global:
            case Type.Argument:
            case Type.GlobalProc:
                return $"{RefType}({Index})";

            case Type.SrcField:
            case Type.Field:
            case Type.SrcProc:
                return $"{RefType}(\"{Name}\")";

            default: return RefType.ToString();
        }
    }
}
