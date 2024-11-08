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
    Instance = 0x2000, // For proc return types
    Path = 0x4000,

    //Byond here be dragons
    Unimplemented = 0x8000, // Marks that a method or property is not implemented. Throws a compiler warning if accessed.
    CompiletimeReadonly = 0x10000, // Marks that a property can only ever be read from, never written to. This is a const-ier version of const, for certain standard values like list.type
}

/// <summary>
/// Allows for more complex things than DMValueType does, such as supporting type paths
/// </summary>
public readonly struct DMComplexValueType {
    public readonly DMValueType Type;
    public readonly DreamPath? TypePath;
    /// <summary>
    /// The indices of the proc parameters used to infer proc return types.
    /// The resulting type is the union of all the parameter types with `this.Type`.
    /// If two or more of those types have paths, their common ancestor is used.
    /// </summary>
    public readonly (int, bool)[]? ParameterIndices;

    public bool IsAnything => Type == DMValueType.Anything;
    public bool IsInstance => Type.HasFlag(DMValueType.Instance);
    public bool HasPath => Type.HasFlag(DMValueType.Path) | Type.HasFlag(DMValueType.Instance);
    public bool IsUnimplemented { get; }
    public bool IsCompileTimeReadOnly { get; }
    public bool IsList => IsInstance && TypePath == DreamPath.List;
    /// <summary>
    /// A pointer to a class wrapping the key and value DMComplexValueTypes for a list.
    /// This cannot be a struct because that would create a cycle in the struct representation.
    /// Sorry about the heap allocation.
    /// </summary>
    public readonly DMListValueTypes? ListValueTypes;

    public DMComplexValueType(DMValueType type, DreamPath? typePath) {
        Type = type & ~(DMValueType.Unimplemented | DMValueType.CompiletimeReadonly); // Ignore these 2 types
        TypePath = typePath;
        IsUnimplemented = type.HasFlag(DMValueType.Unimplemented);
        IsCompileTimeReadOnly = type.HasFlag(DMValueType.CompiletimeReadonly);

        if (HasPath && TypePath == null)
            throw new Exception("A Path or Instance value type must have a type-path");
    }

    public DMComplexValueType(DMValueType type, DreamPath? typePath, (int, bool)[]? parameterIndices) : this(type, typePath) {
        ParameterIndices = parameterIndices;
    }

    public DMComplexValueType(DMValueType type, DreamPath? typePath, (int, bool)[]? parameterIndices, DMListValueTypes? listValueTypes) : this(type, typePath, parameterIndices) {
        ListValueTypes = listValueTypes;
    }

    public DMComplexValueType(DMValueType type, DreamPath? typePath, DMListValueTypes? listValueTypes) : this(type, typePath, null, listValueTypes) { }

    public bool MatchesType(DMValueType type) {
        if (IsAnything || (Type & type) != 0) return true;
        if ((type & (DMValueType.Text | DMValueType.Message)) != 0 && (Type & (DMValueType.Text | DMValueType.Message)) != 0) return true;
        if ((type & (DMValueType.Text | DMValueType.Color)) != 0 && (Type & (DMValueType.Text | DMValueType.Color)) != 0) return true;
        return false;
    }

    public bool MatchesType(DMComplexValueType type) {
        // Exclude checking path and null here; primitives only.
        if (MatchesType(type.Type & ~(DMValueType.Path|DMValueType.Instance|DMValueType.Null)))
            return true;
        // If we have a /icon, we have an icon; if we have a /obj, we have an obj; etc.
        if (IsInstance) {
            if (type.MatchesType(TypePath!.Value.GetAtomType())) {
                return true;
            }
            var theirPath = type.AsPath();
            if (theirPath is not null) {
                var theirObject = DMObjectTree.GetDMObject(theirPath!.Value, false);
                if (theirObject?.IsSubtypeOf(TypePath!.Value) is true) {
                    return true;
                }
            }
        }
        // special case for color and lists:
        if (type.Type.HasFlag(DMValueType.Color) && IsList && ListValueTypes?.NestedListKeyType.Type == DMValueType.Num && ListValueTypes.NestedListValType is null)
            return true;
        // probably only one of these is correct but i can't be assed to figure out which
        if (Type.HasFlag(DMValueType.Color) && type.IsList && type.ListValueTypes?.NestedListKeyType.Type == DMValueType.Num && type.ListValueTypes.NestedListValType is null)
            return true;
        if (type.IsInstance && MatchesType(type.TypePath!.Value.GetAtomType())) {
            return true;
        }
        if (HasPath && type.HasPath) {
            var dmObject = DMObjectTree.GetDMObject(type.TypePath!.Value, false);

            // Allow subtypes
            if (dmObject?.IsSubtypeOf(TypePath!.Value) is false) {
                var ourObject = DMObjectTree.GetDMObject(TypePath!.Value, false);
                return ourObject?.IsSubtypeOf(type.TypePath!.Value) ?? false;
            }
            // If ListValueTypes is non-null, we do more advanced checks.
            if (TypePath!.Value == DreamPath.List && ListValueTypes is not null && type.ListValueTypes is not null) {
                // Have to do an actual match check here. This can get expensive, but thankfully it's pretty rare.
                if (!ListValueTypes.NestedListKeyType.MatchesType(type.ListValueTypes!.NestedListKeyType))
                    return false;
                // If we're assoc (have value types rather than just keys), then the other list must match as well.
                if (ListValueTypes?.NestedListValType is not null) {
                    if (type.ListValueTypes!.NestedListValType is not null && !ListValueTypes.NestedListValType!.Value.MatchesType(type.ListValueTypes!.NestedListValType.Value))
                        return false;
                    if (type.ListValueTypes!.NestedListValType is null && !ListValueTypes.NestedListValType!.Value.MatchesType(DMValueType.Null))
                        return false;
                }
            }
        }
        return MatchesType(type.Type);
    }

    public override string ToString() {
        var types = Type.ToString().ToLowerInvariant();

        return $"\"{(HasPath ? types + $", {TypePath!.Value}{((IsList && ListValueTypes is not null) ? $"({ListValueTypes})" : "")}" : types)}\"";
    }

    public static implicit operator DMComplexValueType(DMValueType type) => new(type, null);
    public static implicit operator DMComplexValueType(DreamPath path) => new(DMValueType.Instance, path);

    public static DMComplexValueType operator |(DMComplexValueType type1, DMValueType type2) =>
        new(type1.Type | type2, type1.TypePath);

    public static DMComplexValueType operator |(DMComplexValueType type1, DMComplexValueType type2) {
        if (type2.TypePath is null) {
            return type1 | type2.Type;
        } else if (type1.TypePath is null) {
            return type2 | type1.Type;
        }
        // Take the common ancestor of both types
        return new(type1.Type | type2.Type, type1.TypePath.Value.GetLastCommonAncestor(type2.TypePath.Value));
    }

    public DreamPath? AsPath() {
        return (HasPath ? TypePath : null) ?? (Type & ~DMValueType.Null) switch {
            DMValueType.Mob => DreamPath.Mob,
            DMValueType.Icon => DreamPath.Icon,
            DMValueType.Obj => DreamPath.Obj,
            DMValueType.Turf => DreamPath.Turf,
            DMValueType.Area => DreamPath.Area,
            DMValueType.Obj | DMValueType.Mob => DreamPath.Movable,
            DMValueType.Area | DMValueType.Turf | DMValueType.Obj | DMValueType.Mob => DreamPath.Atom,
            DMValueType.Sound => DreamPath.Sound,
            _ => null
        };
    }
}

public class DMListValueTypes(DMComplexValueType nestedListKeyType, DMComplexValueType? nestedListValType) {
    public DMComplexValueType NestedListKeyType => nestedListKeyType;
    public DMComplexValueType? NestedListValType => nestedListValType;
    public static DMListValueTypes operator |(DMListValueTypes type1, DMListValueTypes type2) {
        return new(type1.NestedListKeyType | type2.NestedListKeyType, type1.NestedListValType | type2.NestedListValType);
    }
    public override string ToString() {
        if (NestedListValType is not null)
            return $"{NestedListKeyType} = {NestedListValType}";
        return NestedListKeyType.ToString();
    }
}
