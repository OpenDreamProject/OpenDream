using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    // x.y.z
    class Dereference : LValue {
        // Kind of a lazy port
        DMExpression _expr;
        public readonly string PropertyName;
        bool _conditional;

        public override DreamPath? Path => _path;
        DreamPath? _path;

        internal DMExpression Expr => _expr;

        public static bool DirectConvertable(DMExpression expr, DMASTDereference astNode) {
            switch (astNode.Expression) {
                case DMASTDereference deref when deref.Type == DMASTDereference.DereferenceType.Search:
                case DMASTProcCall when expr.Path == null:
                case DMASTDereferenceProc:
                case DMASTList:
                case DMASTListIndex:
                case DMASTTernary:
                case DMASTBinaryAnd:
                    return true;
                case DMASTDereference deref when expr is Dereference _deref:
                    return DirectConvertable(_deref._expr, deref);
                default: return false;
            }
        }

        public Dereference(Location location, DreamPath? path, DMExpression expr, bool conditional, string propertyName)
            : base(location, null)
        {
            _expr = expr;
            _conditional = conditional;
            PropertyName = propertyName;
            _path = path;
        }

        public override void EmitPushInitial(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.PushString(PropertyName);
            proc.Initial();
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.PushString(PropertyName);
            proc.IsSaved();
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            return (DMReference.CreateField(PropertyName), _conditional);
        }

        public override bool TryAsConstant(out Constant constant)
        {
            if(_expr.Path is not null)
            {
                var obj = DMObjectTree.GetDMObject(_expr.Path.GetValueOrDefault());
                var variable = obj.GetVariable(PropertyName);
                if (variable != null)
                {
                    if(variable.IsConst)
                        return variable.Value.TryAsConstant(out constant);
                    if((variable.ValType & DMValueType.CompiletimeReadonly) == DMValueType.CompiletimeReadonly)
                    {
                        variable.Value.TryAsConstant(out constant);
                        return true; // MUST be true.
                    }
                }
            }

            constant = null;
            return false;
        }
    }

    // x.y.z()
    class DereferenceProc : DMExpression {
        // Kind of a lazy port
        DMExpression _expr;
        bool _conditional;
        string _field;

        public DereferenceProc(Location location, DMExpression expr, DMASTDereferenceProc astNode) : base(location) {
            _expr = expr;
            _conditional = astNode.Conditional;
            _field = astNode.Property;

            if (astNode.Type == DMASTDereference.DereferenceType.Direct) {
                if (Dereference.DirectConvertable(expr, astNode)) {
                    astNode.Type = DMASTDereference.DereferenceType.Search;
                    return;
                }
                else if (expr.Path == null) {
                    throw new CompileErrorException(astNode.Location,$"Invalid property \"{_field}\"");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value, false);
                if (dmObject == null) throw new CompileErrorException(Location, $"Type {expr.Path.Value} does not exist");
                if (!dmObject.HasProc(_field)) throw new CompileErrorException(Location, $"Type {_expr.Path.Value} does not have a proc named \"{_field}\"");
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location, "attempt to use proc as value");
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            return (DMReference.CreateProc(_field), _conditional);
        }

        public (DMObject ProcOwner, DMProc Proc) GetProc() {
            if (_expr.Path == null) return (null, null);

            DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value);
            var procId = dmObject.GetProcs(_field)?[^1];
            return (dmObject, procId is null ? null : DMObjectTree.AllProcs[procId.Value]);
        }
    }

    // x[y]
    class ListIndex : LValue {
        DMExpression _expr;
        DMExpression _index;
        bool _conditional;

        public ListIndex(Location location, DMExpression expr, DMExpression index, DreamPath? path, bool conditional)
            : base(location, path)
        {
            _expr = expr;
            _index = index;
            _conditional = conditional;
        }

        public override (DMReference Reference, bool Conditional) EmitReference(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            _index.EmitPushValue(dmObject, proc);

            return (DMReference.ListIndex, _conditional);
        }

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
                DMCompiler.Warning(new CompilerWarning(Location, "calling initial() on a list index returns the current value"));
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
                DMCompiler.Warning(new CompilerWarning(_expr.Location, "calling issaved() on a list index is always false"));
                proc.PushFloat(0);
            }
        }
    }
}
