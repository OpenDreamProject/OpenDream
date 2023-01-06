using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;
using System;

namespace DMCompiler.DM.Expressions {
    // x.y.z
    // x[y][z]
    // x.f().y.g()[2]
    // etc.
    class Dereference : LValue {
        public struct Operation {
            public DMASTDereference.OperationKind Kind;

            // Field*, Call*
            public string Identifier;

            // Field*
            public int? GlobalId;

            // Index*
            public DMExpression Index;

            // Call*
            public ArgumentList Parameters;

            public DreamPath? Path;
        }

        DMExpression _expression;
        Operation[] _operations;

        public override DreamPath? Path => _path;
        DreamPath? _path;

        public Dereference(Location location, DreamPath? path, DMExpression expression, Operation[] operations)
            : base(location, null) {
            _expression = expression;
            _operations = operations;
            _path = path;

            if (_operations.Length == 0) {
                throw new System.InvalidOperationException("deref expression has no operations");
            }
        }

        private void EmitOperation(DMObject dmObject, DMProc proc, ref Operation operation, string endLabel) {
            switch (operation.Kind) {
                case DMASTDereference.OperationKind.Field:
                case DMASTDereference.OperationKind.FieldSearch:
                    proc.DereferenceField(operation.Identifier);
                    break;

                case DMASTDereference.OperationKind.FieldSafe:
                case DMASTDereference.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.DereferenceField(operation.Identifier);
                    break;

                case DMASTDereference.OperationKind.Index:
                    operation.Index.EmitPushValue(dmObject, proc);
                    proc.DereferenceIndex();
                    break;

                case DMASTDereference.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Index.EmitPushValue(dmObject, proc);
                    proc.DereferenceIndex();
                    break;

                case DMASTDereference.OperationKind.Call:
                case DMASTDereference.OperationKind.CallSearch:
                    operation.Parameters.EmitPushArguments(dmObject, proc);
                    proc.DereferenceCall(operation.Identifier);
                    break;

                case DMASTDereference.OperationKind.CallSafe:
                case DMASTDereference.OperationKind.CallSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Parameters.EmitPushArguments(dmObject, proc);
                    proc.DereferenceCall(operation.Identifier);
                    break;

                case DMASTDereference.OperationKind.Invalid:
                default:
                    throw new NotImplementedException();
            };
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            _expression.EmitPushValue(dmObject, proc);

            foreach (ref var operation in _operations.AsSpan()) {
                EmitOperation(dmObject, proc, ref operation, endLabel);
            }

            proc.AddLabel(endLabel);
        }

        public override DMReference EmitReference(DMObject dmObject, DMProc proc, string endLabel) {
            _expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, ref _operations[i], endLabel);
            }

            ref var operation = ref _operations[^1];

            switch (operation.Kind) {
                case DMASTDereference.OperationKind.Field:
                case DMASTDereference.OperationKind.FieldSearch:
                    return DMReference.CreateField(operation.Identifier);

                case DMASTDereference.OperationKind.FieldSafe:
                case DMASTDereference.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    return DMReference.CreateField(operation.Identifier);

                case DMASTDereference.OperationKind.Index:
                    operation.Index.EmitPushValue(dmObject, proc);
                    return DMReference.ListIndex;

                case DMASTDereference.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Index.EmitPushValue(dmObject, proc);
                    return DMReference.ListIndex;

                case DMASTDereference.OperationKind.Call:
                case DMASTDereference.OperationKind.CallSearch:
                case DMASTDereference.OperationKind.CallSafe:
                case DMASTDereference.OperationKind.CallSafeSearch:
                    throw new CompileErrorException(Location, $"attempt to reference proc call result");

                case DMASTDereference.OperationKind.Invalid:
                default:
                    throw new NotImplementedException();
            };
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            _expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, ref _operations[i], endLabel);
            }

            ref var operation = ref _operations[^1];

            switch (operation.Kind) {
                case DMASTDereference.OperationKind.Field:
                case DMASTDereference.OperationKind.FieldSearch:
                    proc.PushString(operation.Identifier);
                    proc.Initial();
                    break;

                case DMASTDereference.OperationKind.FieldSafe:
                case DMASTDereference.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.PushString(operation.Identifier);
                    proc.Initial();
                    break;

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            if (_expr is Dereference { Expr: var derefExpr, PropertyName: "vars" }) {
                derefExpr.EmitPushValue(dmObject, proc);
                _index.EmitPushValue(dmObject, proc);
                proc.Initial();
            } else if (_expr is Field { Variable: { Name: "vars" } }) {
                proc.PushReferenceValue(DMReference.Src);
                _index.EmitPushValue(dmObject, proc);
                proc.Initial();
            } else {
                // This happens silently in BYOND
                DMCompiler.Emit(WarningCode.PointlessBuiltinCall, Location, "calling initial() on a list index returns the current value");
                EmitPushValue(dmObject, proc);
            }
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc)
        {
            if (_expr is Dereference { Expr: var derefExpr, PropertyName: "vars" }) {
                derefExpr.EmitPushValue(dmObject, proc);
                _index.EmitPushValue(dmObject, proc);
                proc.IsSaved();
            } else if (_expr is Field { Variable: { Name: "vars" } }) {
                proc.PushReferenceValue(DMReference.Src);
                _index.EmitPushValue(dmObject, proc);
                proc.IsSaved();
            } else {
                // Silent in BYOND
                DMCompiler.Emit(WarningCode.PointlessBuiltinCall, _expr.Location, "calling issaved() on a list index is always false");
                proc.PushFloat(0);
            }
        }
    }
}
