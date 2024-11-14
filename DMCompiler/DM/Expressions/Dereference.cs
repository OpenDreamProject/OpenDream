using DMCompiler.Bytecode;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions;

// x.y.z
// x[y][z]
// x.f().y.g()[2]
// etc.
internal class Dereference : LValue {
    public abstract class Operation {
        /// <summary>
        /// Whether this operation will short circuit if the dereference equals null. (equal to x?.y)
        /// </summary>
        public required bool Safe { get; init; }

        /// <summary>
        /// The path of the l-value being dereferenced.
        /// </summary>
        public DreamPath? Path { get; init; }
    }

    public abstract class NamedOperation : Operation {
        /// <summary>
        /// The name of the identifier.
        /// </summary>
        public required string Identifier { get; init; }
    }

    public sealed class FieldOperation : NamedOperation;

    public sealed class IndexOperation : Operation {
        /// <summary>
        /// The index expression. (eg. x[expr])
        /// </summary>
        public required DMExpression Index { get; init; }
    }

    public sealed class CallOperation : NamedOperation {
        /// <summary>
        /// The argument list inside the call operation's parentheses. (eg. x(args, ...))
        /// </summary>
        public required ArgumentList Parameters { get; init; }
    }

    public override DreamPath? Path { get; }
    public override DreamPath? NestedPath { get; }
    public override bool PathIsFuzzy => Path == null;
    public override DMComplexValueType ValType { get; }

    private readonly DMExpression _expression;
    private readonly Operation[] _operations;

    public Dereference(DMObjectTree objectTree, Location location, DreamPath? path, DMExpression expression, Operation[] operations)
        : base(location, null) {
        _expression = expression;
        Path = path;
        _operations = operations;

        if (_operations.Length == 0) {
            throw new InvalidOperationException("deref expression has no operations");
        }

        NestedPath = _operations[^1].Path;
        ValType = DetermineValType(objectTree);
    }

    private DMComplexValueType DetermineValType(DMObjectTree objectTree) {
        var type = _expression.ValType;
        var i = 0;
        while (!type.IsAnything && i < _operations.Length) {
            var operation = _operations[i++];

            if (type.TypePath is null || !objectTree.TryGetDMObject(type.TypePath.Value, out var dmObject)) {
                // We're dereferencing something without a type-path, this could be anything
                type = DMValueType.Anything;
                break;
            }

            type = operation switch {
                FieldOperation fieldOperation => dmObject.GetVariable(fieldOperation.Identifier)?.ValType ?? DMValueType.Anything,
                IndexOperation => DMValueType.Anything, // Lists currently can't be typed, this could be anything
                CallOperation callOperation => dmObject.GetProcReturnTypes(callOperation.Identifier) ?? DMValueType.Anything,
                _ => throw new InvalidOperationException("Unimplemented dereference operation")
            };
        }

        return type;
    }

    private void ShortCircuitHandler(DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode) {
        switch (shortCircuitMode) {
            case ShortCircuitMode.PopNull:
                proc.JumpIfNull(endLabel);
                break;
            case ShortCircuitMode.KeepNull:
                proc.JumpIfNullNoPop(endLabel);
                break;
            default:
                throw new InvalidOperationException();
        }
    }

    private void EmitOperation(DMCompiler compiler, DMObject dmObject, DMProc proc, Operation operation, string endLabel, ShortCircuitMode shortCircuitMode) {
        if (operation.Safe) {
            ShortCircuitHandler(proc, endLabel, shortCircuitMode);
        }

        switch (operation) {
            case FieldOperation fieldOperation:
                proc.DereferenceField(fieldOperation.Identifier);
                break;

            case IndexOperation indexOperation:
                if (NestedPath is not null) {
                    if (compiler.DMObjectTree.TryGetDMObject(NestedPath.Value, out var obj) && obj.IsSubtypeOf(DreamPath.Datum) && !obj.HasProc("operator[]")) {
                        compiler.Emit(WarningCode.InvalidIndexOperation, Location, "Invalid index operation. datum[] index operations are not valid starting in BYOND 515.1641");
                    }
                }

                indexOperation.Index.EmitPushValue(compiler, dmObject, proc);
                proc.DereferenceIndex();
                break;

            case CallOperation callOperation:
                var (argumentsType, argumentStackSize) = callOperation.Parameters.EmitArguments(compiler, dmObject, proc, null);
                proc.DereferenceCall(callOperation.Identifier, argumentsType, argumentStackSize);
                break;

        default:
            throw new InvalidOperationException("Unimplemented dereference operation");
        }
    }

    public override void EmitPushValue(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        _expression.EmitPushValue(compiler, dmObject, proc);

        foreach (var operation in _operations) {
            EmitOperation(compiler, dmObject, proc, operation, endLabel, ShortCircuitMode.KeepNull);
        }

        proc.AddLabel(endLabel);
    }

    public override bool CanReferenceShortCircuit() {
        return _operations.Any(operation => operation.Safe);
    }

    public override DMReference EmitReference(DMCompiler compiler, DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
        _expression.EmitPushValue(compiler, dmObject, proc);

        // Perform all except for our last operation
        for (int i = 0; i < _operations.Length - 1; i++) {
            EmitOperation(compiler, dmObject, proc, _operations[i], endLabel, shortCircuitMode);
        }

        var operation = _operations[^1];

        switch (operation) {
            case FieldOperation fieldOperation:
                if (fieldOperation.Safe) {
                    ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                }

                return DMReference.CreateField(fieldOperation.Identifier);

            case IndexOperation indexOperation:
                if (NestedPath is not null) {
                    if (compiler.DMObjectTree.TryGetDMObject(NestedPath.Value, out var obj) && obj.IsSubtypeOf(DreamPath.Datum) && !obj.HasProc("operator[]=")) {
                        compiler.Emit(WarningCode.InvalidIndexOperation, Location, "Invalid index operation. datum[] index operations are not valid starting in BYOND 515.1641");
                    }
                }

                if (indexOperation.Safe) {
                    ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                }

                indexOperation.Index.EmitPushValue(compiler, dmObject, proc);
                return DMReference.ListIndex;

            case CallOperation:
                compiler.Emit(WarningCode.BadExpression, Location,
                    "Expected field or index as reference, got proc call result");
                return default;

            default:
                throw new InvalidOperationException("Unimplemented dereference operation");
        }
    }

    public override void EmitPushInitial(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        if (_expression is LValue exprLValue) {
            // We don't want this instead pushing the constant value if it's const
            exprLValue.EmitPushValueNoConstant(compiler, dmObject, proc);
        } else {
            _expression.EmitPushValue(compiler, dmObject, proc);
        }

        // Perform all except for our last operation
        for (int i = 0; i < _operations.Length - 1; i++) {
            EmitOperation(compiler, dmObject, proc, _operations[i], endLabel, ShortCircuitMode.KeepNull);
        }

        var operation = _operations[^1];

        switch (operation) {
            case FieldOperation fieldOperation:
                if (fieldOperation.Safe) {
                    proc.JumpIfNullNoPop(endLabel);
                }

                proc.PushString(fieldOperation.Identifier);
                proc.Initial();
                break;

            case IndexOperation indexOperation:
                if (indexOperation.Safe) {
                    proc.JumpIfNullNoPop(endLabel);
                }

                indexOperation.Index.EmitPushValue(compiler, dmObject, proc);
                proc.Initial();
                break;

            case CallOperation:
                compiler.Emit(WarningCode.BadExpression, Location,
                    "Expected field or index for initial(), got proc call result");
                break;

            default:
                throw new InvalidOperationException("Unimplemented dereference operation");
        }

        proc.AddLabel(endLabel);
    }

    public void EmitPushIsSaved(DMCompiler compiler, DMObject dmObject, DMProc proc) {
        string endLabel = proc.NewLabelName();

        if (_expression is LValue exprLValue) {
            // We don't want this instead pushing the constant value if it's const
            exprLValue.EmitPushValueNoConstant(compiler, dmObject, proc);
        } else {
            _expression.EmitPushValue(compiler, dmObject, proc);
        }

        // Perform all except for our last operation
        for (int i = 0; i < _operations.Length - 1; i++) {
            EmitOperation(compiler, dmObject, proc, _operations[i], endLabel, ShortCircuitMode.KeepNull);
        }

        var operation = _operations[^1];

        switch (operation) {
            case FieldOperation fieldOperation:
                if (fieldOperation.Safe) {
                    proc.JumpIfNullNoPop(endLabel);
                }

                proc.PushString(fieldOperation.Identifier);
                proc.IsSaved();
                break;

            case IndexOperation indexOperation:
                if (indexOperation.Safe) {
                    proc.JumpIfNullNoPop(endLabel);
                }

                indexOperation.Index.EmitPushValue(compiler, dmObject, proc);
                proc.IsSaved();
                break;

            case CallOperation:
                compiler.Emit(WarningCode.BadExpression, Location,
                    "Expected field or index for issaved(), got proc call result");
                break;

            default:
                throw new InvalidOperationException("Unimplemented dereference operation");
        }

        proc.AddLabel(endLabel);
    }

    // BYOND says the nameof is invalid if the chain is not purely field operations
    public override string? GetNameof(DMCompiler compiler, DMObject dmObject) {
        return _operations.All(op => op is FieldOperation)
            ? ((FieldOperation)_operations[^1]).Identifier
            : null;
    }

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        var prevPath = _operations.Length == 1 ? _expression.Path : _operations[^2].Path;

        var operation = _operations[^1];

        if (operation is FieldOperation fieldOperation && prevPath is not null && compiler.DMObjectTree.TryGetDMObject(prevPath.Value, out var obj)) {
            var variable = obj.GetVariable(fieldOperation.Identifier);
            if (variable != null) {
                if (variable.IsConst)
                    return variable.Value.TryAsConstant(compiler, out constant);
                if (variable.ValType.IsCompileTimeReadOnly) {
                    variable.Value.TryAsConstant(compiler, out constant!);
                    return true; // MUST be true.
                }
            }
        }

        constant = null;
        return false;
    }
}

// expression::identifier
// Same as initial(expression?.identifier) except this keeps its type
internal sealed class ScopeReference(DMObjectTree objectTree, Location location, DMExpression expression, string identifier, DMVariable dmVar)
    : Initial(location, new Dereference(objectTree, location, dmVar.Type, expression, // Just a little hacky
        [
            new Dereference.FieldOperation {
                Identifier = identifier,
                Path = dmVar.Type,
                Safe = true
            }
        ])
    ) {
    public override DreamPath? Path => Expression.Path;

    public override string? GetNameof(DMCompiler compiler, DMObject dmObject) => dmVar.Name;

    public override bool TryAsConstant(DMCompiler compiler, [NotNullWhen(true)] out Constant? constant) {
        if (expression is not IConstantPath) {
            constant = null;
            return false;
        }

        return dmVar.Value!.TryAsConstant(compiler, out constant);
    }
}
