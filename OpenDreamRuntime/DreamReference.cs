using DMRefType = DMCompiler.Bytecode.DMReference.Type;

namespace OpenDreamRuntime;

public readonly struct DreamReference(DMRefType type, int value) : IEquatable<DreamReference> {
    public static readonly DreamReference NoRef = new(DMRefType.NoRef, 0);
    public static readonly DreamReference Src = new(DMRefType.Src, 0);
    public static readonly DreamReference Self = new(DMRefType.Self, 0);
    public static readonly DreamReference Usr = new(DMRefType.Usr, 0);
    public static readonly DreamReference Args = new(DMRefType.Args, 0);
    public static readonly DreamReference SuperProc = new(DMRefType.SuperProc, 0);
    public static readonly DreamReference ListIndex = new(DMRefType.ListIndex, 0);

    public DMRefType Type => (DMRefType)(_innerValue >> 32);
    public int Value => (int)_innerValue;

    private readonly long _innerValue = ((long)type << 32) | (uint)value;

    public static DreamReference CreateArgument(int index) {
        return new DreamReference(DMRefType.Argument, index);
    }

    public static DreamReference CreateLocal(int index) {
        return new DreamReference(DMRefType.Local, index);
    }

    public static DreamReference CreateGlobal(int index) {
        return new DreamReference(DMRefType.Global, index);
    }

    public static DreamReference CreateGlobalProc(int index) {
        return new DreamReference(DMRefType.GlobalProc, index);
    }

    public static DreamReference CreateField(int nameId) {
        return new DreamReference(DMRefType.Field, nameId);
    }

    public static DreamReference CreateSrcField(int nameId) {
        return new DreamReference(DMRefType.SrcField, nameId);
    }

    public static DreamReference CreateSrcProc(int nameId) {
        return new DreamReference(DMRefType.SrcProc, nameId);
    }

    public bool Equals(DreamReference other) {
        return _innerValue == other._innerValue;
    }

    public override bool Equals(object? obj) {
        return obj is DreamReference other && Equals(other);
    }

    public override int GetHashCode() {
        return _innerValue.GetHashCode();
    }

    public static bool operator ==(DreamReference a, DreamReference b) => a._innerValue == b._innerValue;
    public static bool operator !=(DreamReference a, DreamReference b) => a._innerValue != b._innerValue;
}
