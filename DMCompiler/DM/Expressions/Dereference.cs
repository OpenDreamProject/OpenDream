using DMCompiler.Bytecode;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DMCompiler.Compiler;

namespace DMCompiler.DM.Expressions {
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

        public readonly DMExpression Expression;
        public override DreamPath? Path { get; }
        public override DreamPath? NestedPath { get; }
        public override bool PathIsFuzzy => Path == null;

        private readonly Operation[] _operations;
        private DMValueType? _valType;

        public override DMValueType ValType {
            get => _valType ?? DMValueType.Anything;
            /*init {
                foreach (var operation in _operations) {
                    switch (operation) {
                        case FieldOperation fieldOperation:
                            ValType = dmObject.GetVariable(fieldOperation.Identifier)?.ValType ?? DMValueType.Anything;
                            break;

                        case CallOperation callOperation:
                            if (callOperation.Safe) {
                                ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                            }

                            ValType |= proc.ReturnTypes;
                            break;
                    }
                }
            }*/
        }

        public Dereference(Location location, DreamPath? path, DMExpression expression, Operation[] operations)
            : base(location, null) {
            Expression = expression;
            Path = path;
            _operations = operations;

            if (_operations.Length == 0) {
                throw new InvalidOperationException("deref expression has no operations");
            }

            NestedPath = _operations[^1].Path;
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

        private void EmitOperation(DMObject dmObject, DMProc proc, Operation operation, string endLabel, ShortCircuitMode shortCircuitMode) {
            switch (operation) {
                case FieldOperation fieldOperation:
                    var valType = dmObject.GetVariable(fieldOperation.Identifier)?.ValType ?? DMValueType.Anything;
                    if (valType != DMValueType.Anything) {
                        Console.WriteLine("foo");
                    }
                    if (fieldOperation.Safe) {
                        ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                    }
                    proc.DereferenceField(fieldOperation.Identifier);
                    break;

                case IndexOperation indexOperation:
                    if (indexOperation.Safe) {
                        ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                    }
                    indexOperation.Index.EmitPushValue(dmObject, proc);
                    proc.DereferenceIndex();
                    break;

                case CallOperation callOperation:
                    if (callOperation.Safe) {
                        ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                    }
                    //ValType = proc.ReturnTypes;
                    var (argumentsType, argumentStackSize) = callOperation.Parameters.EmitArguments(dmObject, proc);
                    proc.DereferenceCall(callOperation.Identifier, argumentsType, argumentStackSize);
                    break;

                default:
                    throw new InvalidOperationException("Unimplemented dereference operation");
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            Expression.EmitPushValue(dmObject, proc);

            foreach (var operation in _operations) {
                EmitOperation(dmObject, proc, operation, endLabel, ShortCircuitMode.KeepNull);
            }

            proc.AddLabel(endLabel);
        }

        public override bool CanReferenceShortCircuit() {
            return _operations.Any(operation => operation.Safe);
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
            Expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, _operations[i], endLabel, shortCircuitMode);
            }

            var operation = _operations[^1];

            switch (operation) {
                case FieldOperation fieldOperation:
                    if (fieldOperation.Safe) {
                        ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                    }
                    return DMReference.CreateField(fieldOperation.Identifier);

                case IndexOperation indexOperation:
                    if (indexOperation.Safe) {
                        ShortCircuitHandler(proc, endLabel, shortCircuitMode);
                    }
                    indexOperation.Index.EmitPushValue(dmObject, proc);
                    return DMReference.ListIndex;

                case CallOperation:
                    DMCompiler.Emit(WarningCode.BadExpression, Location,
                        "Expected field or index as reference, got proc call result");
                    return default;

                default:
                    throw new InvalidOperationException("Unimplemented dereference operation");
            }
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            Expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, _operations[i], endLabel, ShortCircuitMode.KeepNull);
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
                    indexOperation.Index.EmitPushValue(dmObject, proc);
                    proc.Initial();
                    break;

                case CallOperation:
                    DMCompiler.Emit(WarningCode.BadExpression, Location,
                        "Expected field or index for initial(), got proc call result");
                    break;

                default:
                    throw new InvalidOperationException("Unimplemented dereference operation");
            }

            proc.AddLabel(endLabel);
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            Expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, _operations[i], endLabel, ShortCircuitMode.KeepNull);
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
                    indexOperation.Index.EmitPushValue(dmObject, proc);
                    proc.IsSaved();
                    break;

                case CallOperation:
                    DMCompiler.Emit(WarningCode.BadExpression, Location,
                        "Expected field or index for issaved(), got proc call result");
                    break;

                default:
                    throw new InvalidOperationException("Unimplemented dereference operation");
            }

            proc.AddLabel(endLabel);
        }

        // BYOND says the nameof is invalid if the chain is not purely field operations
        public override string? GetNameof(DMObject dmObject, DMProc proc) {
            return _operations.All(op => op is FieldOperation)
                ? ((FieldOperation)_operations[^1]).Identifier
                : null;
        }

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            var prevPath = _operations.Length == 1 ? Expression.Path : _operations[^2].Path;

            var operation = _operations[^1];

            if (operation is FieldOperation fieldOperation && prevPath is not null) {
                var obj = DMObjectTree.GetDMObject(prevPath.Value);
                var variable = obj!.GetVariable(fieldOperation.Identifier);
                if (variable != null) {
                    if (variable.IsConst)
                        return variable.Value.TryAsConstant(out constant);
                    if (variable.ValType.HasFlag(DMValueType.CompiletimeReadonly)) {
                        variable.Value.TryAsConstant(out constant!);
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
    internal sealed class ScopeReference(Location location, DMExpression expression, string identifier, DMVariable dmVar)
        : Initial(location, new Dereference(location, dmVar.Type, expression, // Just a little hacky
            new Dereference.Operation[] {
                new Dereference.FieldOperation {
                    Identifier = identifier,
                    Path = dmVar.Type,
                    Safe = true
                }
            })
        ) {
        public override DreamPath? Path => Expression.Path;

        public override string GetNameof(DMObject dmObject, DMProc proc) => dmVar.Name;

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            if (expression is not ConstantPath || dmVar.Value is not Constant varValue) {
                constant = null;
                return false;
            }

            constant = varValue;
            return true;
        }
    }
}
