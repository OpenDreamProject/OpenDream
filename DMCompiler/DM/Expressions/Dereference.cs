using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler;
using DMCompiler.Compiler.DM;
using OpenDreamShared.Dream;
using OpenDreamShared.Dream.Procs;

namespace DMCompiler.DM.Expressions {
    // x.y.z
    class Dereference : LValue {
        // Kind of a lazy port
        DMExpression _expr;
        string _propertyName;
        bool _conditional;

        public override DreamPath? Path => _path;
        DreamPath? _path;

        public static bool DirectConvertable(DMExpression expr, DMASTDereference astNode) {
            switch (astNode.Expression) {
                case DMASTDereference deref when deref.Type == DMASTDereference.DereferenceType.Search:
                case DMASTProcCall when expr.Path == null:
                case DMASTDereferenceProc:
                case DMASTListIndex:
                    return true;
                case DMASTDereference deref when expr is Dereference _deref:
                    return DirectConvertable(_deref._expr, deref);
                default: return false;
            }
        }

        public Dereference(Location location, DMExpression expr, DMASTDereference astNode)
            : base(location, null) // This gets filled in later
        {
            _expr = expr;
            _conditional = astNode.Conditional;
            _propertyName = astNode.Property;

            if (astNode.Type == DMASTDereference.DereferenceType.Direct) {
                if (DirectConvertable(expr, astNode)) {
                    astNode.Type = DMASTDereference.DereferenceType.Search;
                    return;
                }
                else if (expr.Path == null) {
                    throw new CompileErrorException(astNode.Location,$"Invalid property {_propertyName}");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(expr.Path.Value, false);

                var current = dmObject.GetVariable(_propertyName);
                if (current == null) current = dmObject.GetGlobalVariable(_propertyName);
                if (current == null) throw new CompileErrorException(astNode.Location,$"Invalid property \"{_propertyName}\" on type {dmObject.Path}");

                if ((current.Value?.ValType & DMValueType.Unimplemented) == DMValueType.Unimplemented && !DMCompiler.Settings.SuppressUnimplementedWarnings) {
                    DMCompiler.Warning(new CompilerWarning(Location, $"{dmObject.Path}.{_propertyName} is not implemented and will have unexpected behavior"));
                }

                _path = current.Type;
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            if (_conditional) {
                proc.DereferenceConditional(_propertyName);
            } else {
                proc.Dereference(_propertyName);
            }
        }

        public override IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            if (_conditional) {
                proc.DereferenceConditional(_propertyName);
            } else {
                proc.Dereference(_propertyName);
            }

            if (_conditional) {
                return IdentifierPushResult.Conditional;
            } else {
                return IdentifierPushResult.Unconditional;
            }
        }

        public void EmitPushInitial(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.Initial(_propertyName);
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            proc.IsSaved(_propertyName);
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
                    throw new CompileErrorException(astNode.Location,$"Invalid property {_field}");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value, false);
                if (!dmObject.HasProc(_field)) throw new CompileErrorException(Location, $"Type {_expr.Path.Value} does not have a proc named \"{_field}\"");
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException(Location.Unknown,"attempt to use proc as value");
        }

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            if (_conditional) {
                proc.DereferenceProcConditional(_field);
                return ProcPushResult.Conditional;
            } else {
                proc.DereferenceProc(_field);
                return ProcPushResult.Unconditional;
            }
        }

        public DMProc GetProc() {
            if (_expr.Path == null) return null;

            DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value);
            return dmObject.GetProcs(_field)?[^1];
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

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            _index.EmitPushValue(dmObject, proc);

            if (_conditional) {
                proc.IndexListConditional();
            } else {
                proc.IndexList();
            }
        }
    }
}
