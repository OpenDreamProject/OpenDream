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
        List<(bool conditional, string field)> _fields = new();

        public override DreamPath? Path => _path;
        DreamPath? _path;

        public Dereference(DMExpression expr, DMASTDereference astNode, bool includingLast)
            : base(null) // This gets filled in later
        {
            _expr = expr;

            var current_path = _expr.Path;
            DMASTDereference.Dereference[] dereferences = astNode.Dereferences;
            for (int i = 0; i < (includingLast ? dereferences.Length : dereferences.Length - 1); i++) {
                DMASTDereference.Dereference deref = dereferences[i];

                switch (deref.Type) {
                    case DMASTDereference.DereferenceType.Direct: {
                        if (current_path == null) {
                            throw new CompileErrorException("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                        }

                        DMObject dmObject = DMObjectTree.GetDMObject(current_path.Value, false);

                        var current = dmObject.GetVariable(deref.Property);
                        if (current == null) current = dmObject.GetGlobalVariable(deref.Property);
                        if (current == null) throw new CompileErrorException("Invalid property \"" + deref.Property + "\" on type " + dmObject.Path);

                        current_path = current.Type;
                        _fields.Add((deref.Conditional, deref.Property));
                        break;
                    }

                    case DMASTDereference.DereferenceType.Search: {
                        var current = new DMVariable(null, deref.Property, false);
                        current_path = current.Type;
                        _fields.Add((deref.Conditional, deref.Property));
                        break;
                    }

                    default:
                        throw new InvalidOperationException();
                }
            }

            _path = current_path;
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            foreach (var (conditional, field) in _fields) {
                if (conditional) {
                    proc.DereferenceConditional(field);
                } else {
                    proc.Dereference(field);
                }
            }
        }

        public override IdentifierPushResult EmitIdentifier(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            bool lastConditional = false;
            foreach (var (conditional, field) in _fields) {
                if (conditional) {
                    proc.DereferenceConditional(field);
                } else {
                    proc.Dereference(field);
                }

                lastConditional = conditional;
            }

            if (lastConditional) {
                return IdentifierPushResult.Conditional;
            } else {
                return IdentifierPushResult.Unconditional;
            }
        }

        public void EmitPushInitial(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            for (int idx = 0; idx < _fields.Count - 1; idx++)
            {
                if (_fields[idx].conditional) {
                    proc.DereferenceConditional(_fields[idx].field);
                } else {
                    proc.Dereference(_fields[idx].field);
                }
            }

            // TODO: Handle conditional
            proc.Initial(_fields[^1].field);
        }

        public void EmitPushIsSaved(DMObject dmObject, DMProc proc) {
            _expr.EmitPushValue(dmObject, proc);

            for (int idx = 0; idx < _fields.Count - 1; idx++)
            {
                if (_fields[idx].conditional) {
                    proc.DereferenceConditional(_fields[idx].field);
                } else {
                    proc.Dereference(_fields[idx].field);
                }
            }

            // TODO: Handle conditional
            proc.IsSaved(_fields[^1].field);
        }
    }

    // x.y.z()
    class DereferenceProc : DMExpression {
        // Kind of a lazy port
        Dereference _parent;
        bool _conditional;
        string _field;

        public DereferenceProc(DMExpression expr, DMASTDereferenceProc astNode) {
            _parent = new Dereference(expr, astNode, false);

            DMASTDereference.Dereference deref = astNode.Dereferences[^1];
            switch (deref.Type) {
                case DMASTDereference.DereferenceType.Direct: {
                    if (_parent.Path == null) {
                        throw new CompileErrorException("Cannot dereference property \"" + deref.Property + "\" because a type specifier is missing");
                    }

                    DreamPath type = _parent.Path.Value;
                    DMObject dmObject = DMObjectTree.GetDMObject(type, false);

                    if (!dmObject.HasProc(deref.Property)) throw new CompileErrorException("Type + " + type + " does not have a proc named \"" + deref.Property + "\"");
                    _conditional = deref.Conditional;
                    _field = deref.Property;
                    break;
                }

                case DMASTDereference.DereferenceType.Search: {
                    _conditional = deref.Conditional;
                    _field = deref.Property;
                    break;
                }

                default:
                    throw new InvalidOperationException();
            }
        }

        public override void EmitPushValue(DMObject dmObject, DMProc proc) {
            throw new CompileErrorException("attempt to use proc as value");
        }

        public override ProcPushResult EmitPushProc(DMObject dmObject, DMProc proc) {
            _parent.EmitPushValue(dmObject, proc);

            if (_conditional) {
                proc.DereferenceProcConditional(_field);
                return ProcPushResult.Conditional;
            } else {
                proc.DereferenceProc(_field);
                return ProcPushResult.Unconditional;
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
