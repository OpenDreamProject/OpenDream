using System.Diagnostics.CodeAnalysis;
using DMCompiler.DM.Expressions;

namespace DMCompiler.DM;

internal sealed class DMVariable {
    public DreamPath? Type;
    public readonly string Name;
    public readonly bool IsGlobal;
    public readonly bool IsTmp;
    public readonly bool IsFinal;
    public DMExpression? Value;
    public DMComplexValueType ValType;

    /// <remarks>
    /// NOTE: This DMVariable may be forced constant through opendream_compiletimereadonly. This only marks that the variable has the DM quality of /const/ness.
    /// </remarks>
    public readonly bool IsConst;

    private bool CanConstFold => (IsConst || ValType.Type.HasFlag(DMValueType.CompiletimeReadonly)) &&
                                 !ValType.Type.HasFlag(DMValueType.NoConstFold);

    public DMVariable(DreamPath? type, string name, bool isGlobal, bool isConst, bool isFinal, bool isTmp, DMComplexValueType? valType = null) {
        Type = type;
        Name = name;
        IsGlobal = isGlobal;
        IsConst = isConst;
        IsFinal = isFinal;
        IsTmp = isTmp;
        Value = null;
        ValType = valType ?? DMValueType.Anything;
    }

    public DMVariable(DMVariable copyFrom) {
        Type = copyFrom.Type;
        Name = copyFrom.Name;
        IsGlobal = copyFrom.IsGlobal;
        IsConst = copyFrom.IsConst;
        IsFinal = copyFrom.IsFinal;
        IsTmp = copyFrom.IsTmp;
        Value = copyFrom.Value;
        ValType = copyFrom.ValType;
    }

    public bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (CanConstFold && Value != null) {
            return Value.TryAsConstant(compiler, out constant);
        } else {
            constant = null;
            return false;
        }
    }

    public bool TryAsJsonRepresentation(DMCompiler compiler, [NotNullWhen(true)] out object? valueJson) {
        if (Value == null) {
            valueJson = null;
            return false;
        }

        return Value.TryAsJsonRepresentation(compiler, out valueJson);
    }
}
