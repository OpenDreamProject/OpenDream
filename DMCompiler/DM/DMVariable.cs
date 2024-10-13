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

    public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst, bool isTmp, DMComplexValueType? valType = null) {
        Type = type;
        Name = name;
        IsGlobal = isGlobal;
        IsConst = isConst;
        IsTmp = isTmp;
        Value = null;
        ValType = valType ?? DMValueType.Anything;
    }

    /// <summary>
    /// This is a copy-on-write proc used to set the DMVariable to a constant value. <br/>
    /// In some contexts, doing so would clobber pre-existing constants, <br/>
    /// and so this sometimes creates a copy of <see langword="this"/>, with the new constant value.
    /// </summary>
    public DMVariable WriteToValue(Expressions.Constant value) {
        if (Value == null) {
            Value = value;
            return this;
        }

        DMVariable clone = new DMVariable(Type, Name, IsGlobal, IsConst, IsTmp, ValType);
        clone.Value = value;
        return clone;
    }

    public bool TryAsJsonRepresentation([NotNullWhen(true)] out object? valueJson) {
        return Value.TryAsJsonRepresentation(out valueJson);
    }
}
