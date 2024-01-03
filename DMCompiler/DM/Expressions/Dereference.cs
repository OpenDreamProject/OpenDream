using DMCompiler.Bytecode;
using OpenDreamShared.Compiler;
using OpenDreamShared.Dream;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DMCompiler.DM.Expressions {
    // x.y.z
    // x[y][z]
    // x.f().y.g()[2]
    // etc.
    internal class Dereference : LValue {
        public abstract class Operation {
            public required bool Safe { get; init; }
            public DreamPath? Path { get; init; }
        }

        public abstract class NamedOperation : Operation {
            public required string Identifier { get; init; }
        }

        public sealed class FieldOperation : NamedOperation;

        public sealed class IndexOperation : Operation {
            public required DMExpression Index { get; init; }
        }

        public sealed class CallOperation : NamedOperation {
            public required ArgumentList Parameters { get; init; }
        }

        private readonly DMExpression _expression;
        private readonly Operation[] _operations;

        public override DreamPath? Path { get; }
        public override DreamPath? NestedPath { get; }
        public override bool PathIsFuzzy => Path == null;

        public Dereference(Location location, DreamPath? path, DMExpression expression, Operation[] operations)
            : base(location, null) {
            _expression = expression;
            _operations = operations;
            Path = path;

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
                    var (argumentsType, argumentStackSize) = callOperation.Parameters.EmitArguments(dmObject, proc);
                    proc.DereferenceCall(callOperation.Identifier, argumentsType, argumentStackSize);
                    break;

                default:
                    throw new InvalidOperationException("Unimplemented dereference operation");
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            _expression.EmitPushValue(dmObject, proc);

            foreach (var operation in _operations) {
                EmitOperation(dmObject, proc, operation, endLabel, ShortCircuitMode.KeepNull);
            }

            proc.AddLabel(endLabel);
        }

        public override bool CanReferenceShortCircuit() {
            return _operations.Any(operation => operation.Safe);
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel, ShortCircuitMode shortCircuitMode = ShortCircuitMode.KeepNull) {
            _expression.EmitPushValue(dmObject, proc);

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

            _expression.EmitPushValue(dmObject, proc);

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

            _expression.EmitPushValue(dmObject, proc);

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

        public override bool TryAsConstant([NotNullWhen(true)] out Constant? constant) {
            var prevPath = _operations.Length == 1 ? _expression.Path : _operations[^2].Path;

            var operation = _operations[^1];

            switch (operation) {
                case FieldOperation fieldOperation:
                    if (prevPath is not null) {
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
                    break;
            }

            constant = null;
            return false;
        }
    }
}
