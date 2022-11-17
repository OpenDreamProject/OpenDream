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
    class Deref : LValue {
        public struct Operation {
            public DMASTDeref.OperationKind Kind;

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

        public Deref(Location location, DreamPath? path, DMExpression expression, Operation[] operations)
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
                case DMASTDeref.OperationKind.Field:
                case DMASTDeref.OperationKind.FieldSearch:
                    proc.DereferenceField(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.FieldSafe:
                case DMASTDeref.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.DereferenceField(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.Index:
                    operation.Index.EmitPushValue(dmObject, proc);
                    proc.DereferenceIndex();
                    break;

                case DMASTDeref.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Index.EmitPushValue(dmObject, proc);
                    proc.DereferenceIndex();
                    break;

                case DMASTDeref.OperationKind.Call:
                case DMASTDeref.OperationKind.CallSearch:
                    operation.Parameters.EmitPushArguments(dmObject, proc);
                    proc.DereferenceCall(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.CallSafe:
                case DMASTDeref.OperationKind.CallSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Parameters.EmitPushArguments(dmObject, proc);
                    proc.DereferenceCall(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.Invalid:
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
                case DMASTDeref.OperationKind.Field:
                case DMASTDeref.OperationKind.FieldSearch:
                    return DMReference.CreateField(operation.Identifier);

                case DMASTDeref.OperationKind.FieldSafe:
                case DMASTDeref.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    return DMReference.CreateField(operation.Identifier);

                case DMASTDeref.OperationKind.Index:
                    operation.Index.EmitPushValue(dmObject, proc);
                    return DMReference.ListIndex;

                case DMASTDeref.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    operation.Index.EmitPushValue(dmObject, proc);
                    return DMReference.ListIndex;

                case DMASTDeref.OperationKind.Call:
                case DMASTDeref.OperationKind.CallSearch:
                case DMASTDeref.OperationKind.CallSafe:
                case DMASTDeref.OperationKind.CallSafeSearch:
                    throw new CompileErrorException(Location, $"attempt to reference proc call result");

                case DMASTDeref.OperationKind.Invalid:
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
                case DMASTDeref.OperationKind.Field:
                case DMASTDeref.OperationKind.FieldSearch:
                    proc.Initial(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.FieldSafe:
                case DMASTDeref.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.Initial(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.Index:
                    EmitOperation(dmObject, proc, ref operation, endLabel);
                    break;

                case DMASTDeref.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    EmitOperation(dmObject, proc, ref operation, endLabel);
                    break;

                case DMASTDeref.OperationKind.Call:
                case DMASTDeref.OperationKind.CallSearch:
                case DMASTDeref.OperationKind.CallSafe:
                case DMASTDeref.OperationKind.CallSafeSearch:
                    throw new CompileErrorException(Location, $"attempt to get the initial value of a proc call");

                case DMASTDeref.OperationKind.Invalid:
                default:
                    throw new NotImplementedException();
            };

            proc.AddLabel(endLabel);
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc) {
            string endLabel = proc.NewLabelName();

            _expression.EmitPushValue(dmObject, proc);

            // Perform all except for our last operation
            for (int i = 0; i < _operations.Length - 1; i++) {
                EmitOperation(dmObject, proc, ref _operations[i], endLabel);
            }

            ref var operation = ref _operations[^1];

            switch (operation.Kind) {
                case DMASTDeref.OperationKind.Field:
                case DMASTDeref.OperationKind.FieldSearch:
                    proc.IsSaved(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.FieldSafe:
                case DMASTDeref.OperationKind.FieldSafeSearch:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.IsSaved(operation.Identifier);
                    break;

                case DMASTDeref.OperationKind.Index:
                    // TODO: Support "vars" properly (i don't know what that means)
                    proc.Pop();
                    proc.PushFloat(0f);
                    break;

                case DMASTDeref.OperationKind.IndexSafe:
                    proc.JumpIfNullNoPop(endLabel);
                    proc.Pop();
                    proc.PushFloat(0f);
                    break;

                case DMASTDeref.OperationKind.Call:
                case DMASTDeref.OperationKind.CallSearch:
                case DMASTDeref.OperationKind.CallSafe:
                case DMASTDeref.OperationKind.CallSafeSearch:
                    throw new CompileErrorException(Location, $"attempt to get the initial value of a proc call");

                case DMASTDeref.OperationKind.Invalid:
                default:
                    throw new NotImplementedException();
            };

            proc.AddLabel(endLabel);
        }

        public override bool TryAsConstant(out Constant constant) {
            ref var operation = ref _operations[^1];

            switch (operation.Kind) {
                case DMASTDeref.OperationKind.Field:
                case DMASTDeref.OperationKind.FieldSearch:
                    DreamPath? parentPath = (_operations.Length > 1) ? _operations[^2].Path : _expression.Path;

                    if (parentPath != null) {
                        var obj = DMObjectTree.GetDMObject(parentPath.Value);
                        var variable = obj.GetVariable(operation.Identifier);
                        if (variable != null) {
                            if (variable.IsConst)
                                return variable.Value.TryAsConstant(out constant);
                            if ((variable.ValType & DMValueType.CompiletimeReadonly) == DMValueType.CompiletimeReadonly) {
                                variable.Value.TryAsConstant(out constant);
                                return true; // MUST be true.
                            }
                        }
                    }

                    constant = null;
                    return false;

                case DMASTDeref.OperationKind.FieldSafe:
                case DMASTDeref.OperationKind.FieldSafeSearch:
                    constant = null;
                    return false;

                case DMASTDeref.OperationKind.Index:
                    constant = null;
                    return false;

                case DMASTDeref.OperationKind.IndexSafe:
                    constant = null;
                    return false;

                case DMASTDeref.OperationKind.Call:
                case DMASTDeref.OperationKind.CallSearch:
                case DMASTDeref.OperationKind.CallSafe:
                case DMASTDeref.OperationKind.CallSafeSearch:
                    constant = null;
                    return false;

                case DMASTDeref.OperationKind.Invalid:
                default:
                    throw new NotImplementedException();
            };
        }
    }
}
