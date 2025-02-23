using OpenDreamShared.Common;
using OpenDreamShared.Common.DM;

namespace DMCompiler.DM;

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
