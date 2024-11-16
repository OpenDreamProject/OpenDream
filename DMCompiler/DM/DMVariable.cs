using System.Diagnostics.CodeAnalysis;

namespace DMCompiler.DM;

internal sealed class DMVariable {
    public DreamPath? Type;
    public string Name;
    public bool IsGlobal;
    /// <remarks>
    /// NOTE: This DMVariable may be forced constant through opendream_compiletimereadonly. This only marks that the variable has the DM quality of /const/ness.
    /// </remarks>
    public bool IsConst;
    public bool IsTmp;
    public DMExpression? Value;
    public DMComplexValueType ValType;

    public bool CanConstFold => (IsConst || ValType.MatchesType(DMValueType.CompiletimeReadonly)) &&
                                !ValType.MatchesType(DMValueType.NoConstFold);

    public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst, bool isTmp, DMComplexValueType? valType = null) {
        Type = type;
        Name = name;
        IsGlobal = isGlobal;
        IsConst = isConst;
        IsTmp = isTmp;
        Value = null;
        ValType = valType ?? DMValueType.Anything;
    }

    public DMVariable(DMVariable copyFrom) {
        Type = copyFrom.Type;
        Name = copyFrom.Name;
        IsGlobal = copyFrom.IsGlobal;
        IsConst = copyFrom.IsConst;
        IsTmp = copyFrom.IsTmp;
        Value = copyFrom.Value;
        ValType = copyFrom.ValType;
    }

    public bool TryAsJsonRepresentation(DMCompiler compiler, [NotNullWhen(true)] out object? valueJson) {
        return Value.TryAsJsonRepresentation(compiler, out valueJson);
    }
}
