namespace DMCompiler.DM;

// If you are modifying this, you must also modify OpenDreamShared.Dream.DreamValueType !!
// Unfortunately the client needs this and it can't reference DMCompiler due to the sandbox

///<summary>
///Stores any explicit casting done via the "as" keyword. Also stores compiler hints for DMStandard.<br/>
///is a [Flags] enum because it's possible for something to have multiple values (especially with the quirky DMStandard ones)
/// </summary>
[Flags]
public enum DMValueType {
    Anything = 0x0,
    Null = 0x1,
    Text = 0x2,
    Obj = 0x4,
    Mob = 0x8,
    Turf = 0x10,
    Num = 0x20,
    Message = 0x40,
    Area = 0x80,
    Color = 0x100,
    File = 0x200,
    CommandText = 0x400,
    Sound = 0x800,
    Icon = 0x1000,
    Path = 0x2000, // For proc return types

    //Byond here be dragons
    Unimplemented = 0x4000, // Marks that a method or property is not implemented. Throws a compiler warning if accessed.
    CompiletimeReadonly = 0x8000, // Marks that a property can only ever be read from, never written to. This is a const-ier version of const, for certain standard values like list.type
    NoConstFold = 0x10000 // Marks that a const var cannot be const-folded during compile
}

/// <summary>
/// Allows for more complex things than DMValueType does, such as supporting type paths
/// </summary>
public readonly struct DMComplexValueType {
    public readonly DMValueType Type;
    public readonly DreamPath? TypePath;

    public bool IsAnything => Type == DMValueType.Anything;
    public bool IsPath => Type.HasFlag(DMValueType.Path);
    public bool IsUnimplemented { get; }
    public bool IsCompileTimeReadOnly { get; }

    public DMComplexValueType(DMValueType type, DreamPath? typePath) {
        Type = type & ~(DMValueType.Unimplemented | DMValueType.CompiletimeReadonly); // Ignore these 2 types
        TypePath = typePath;
        IsUnimplemented = type.HasFlag(DMValueType.Unimplemented);
        IsCompileTimeReadOnly = type.HasFlag(DMValueType.CompiletimeReadonly);

        if (IsPath && TypePath == null)
            throw new Exception("A Path value type must have a type-path");
    }

    public bool MatchesType(DMValueType type) {
        return IsAnything || (Type & type) != 0;
    }

    internal bool MatchesType(DMCompiler compiler, DMComplexValueType type) {
        if (IsPath && type.IsPath) {
            if (compiler.DMObjectTree.TryGetDMObject(type.TypePath!.Value, out var dmObject) &&
                dmObject.IsSubtypeOf(TypePath!.Value)) // Allow subtypes
                return true;
        }

        return MatchesType(type.Type);
    }

    public override string ToString() {
        var types = Type.ToString().ToLowerInvariant();

        return $"\"{(IsPath ? types + ", " + TypePath!.Value : types)}\"";
    }

    public static implicit operator DMComplexValueType(DMValueType type) => new(type, null);
    public static implicit operator DMComplexValueType(DreamPath path) => new(DMValueType.Path, path);

    public static DMComplexValueType operator |(DMComplexValueType type1, DMValueType type2) =>
        new(type1.Type | type2, type1.TypePath);
}
