using System;
using System.Collections.Generic;
using OpenDreamShared.Compiler;
using OpenDreamShared.Compiler.DM;
using OpenDreamShared.Dream;

namespace DMCompiler.DM.Expressions {
    // x.y.z
    class Dereference : LValue {
        // Kind of a lazy port
        DMExpression _expr;
        string _propertyName;
        bool _conditional;

        public override DreamPath? Path => _path;
        DreamPath? _path;

        public Dereference(DMExpression expr, DMASTDereference astNode)
            : base(null) // This gets filled in later
        {
            _expr = expr;
            _conditional = astNode.Conditional;
            _propertyName = astNode.Property;

            if (astNode.Type == DMASTDereference.DereferenceType.Direct) {
                if (expr.Path == null) {
                    throw new CompileErrorException($"Cannot dereference property \"{_propertyName}\" because a type specifier is missing");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(expr.Path.Value, false);

                var current = dmObject.GetVariable(_propertyName);
                if (current == null) current = dmObject.GetGlobalVariable(_propertyName);
                if (current == null) throw new CompileErrorException($"Invalid property \"{_propertyName}\" on type {dmObject.Path}");

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

        public DereferenceProc(DMExpression expr, DMASTDereferenceProc astNode) {
            _expr = expr;
            _conditional = astNode.Conditional;
            _field = astNode.Property;

            if (astNode.Type == DMASTDereference.DereferenceType.Direct) {
                if (_expr.Path == null) {
                    throw new CompileErrorException($"Cannot dereference property \"{_field}\" because a type specifier is missing");
                }

                DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value, false);
                if (!dmObject.HasProc(_field)) throw new CompileErrorException($"Type {_expr.Path.Value} does not have a proc named \"{_field}\"");
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException("attempt to use proc as value");
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

        public void UnimplementedCheck() {
            if (_expr.Path == null) return;

            DMObject dmObject = DMObjectTree.GetDMObject(_expr.Path.Value);
            if (dmObject.IsProcUnimplemented(_field)) {
                Program.Warning(new CompilerWarning(null, $"{dmObject.Path}.{_field}() is not implemented"));
            }
        }
    }

    // x[y]
    class ListIndex : LValue {
        DMExpression _expr;
        DMExpression _index;

        public ListIndex(DMExpression expr, DMExpression index, DreamPath? path)
            : base(path)
        {
            _expr = expr;
            _index = index;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);
            _index.EmitPushValue(dmObject, proc);
            proc.IndexList();
        }
    }
}
