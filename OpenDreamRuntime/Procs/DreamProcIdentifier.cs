using OpenDreamRuntime.Objects;
using System;
using System.Collections.Generic;

namespace OpenDreamRuntime.Procs {
    interface IDreamProcIdentifier {
        public DreamValue GetValue();
        public void Assign(DreamValue value);
    }

    struct DreamProcIdentifierVariable : IDreamProcIdentifier {
        public DreamObject Instance;
        public string IdentifierName;

        public DreamProcIdentifierVariable(DreamObject instance, string identifierName) {
            Instance = instance;
            IdentifierName = identifierName;
        }

        public DreamValue GetValue() {
            if (Instance.TryGetVariable(IdentifierName, out DreamValue value)) {
                return value;
            }
            if (Instance.ObjectDefinition.GlobalVariables.TryGetValue(IdentifierName, out int globalId)) {
                return Instance.Runtime.Globals[globalId];
            }
            throw new Exception("Value '" + IdentifierName + "' doesn't exist");
        }

        public void Assign(DreamValue value) {
            if (Instance.HasVariable(IdentifierName)) {
                Instance.SetVariable(IdentifierName, value);
            } else if (Instance.ObjectDefinition.GlobalVariables.TryGetValue(IdentifierName, out int globalId)) {
                Instance.Runtime.Globals[globalId] = value;
            } else {
                throw new Exception("Value '" + IdentifierName + "' doesn't exist");
            }
        }
    }

    struct DreamProcIdentifierGlobal : IDreamProcIdentifier {
        public int GlobalId;

        private List<DreamValue> _globals;

        public DreamProcIdentifierGlobal(List<DreamValue> globals, int globalId) {
            _globals = globals;
            GlobalId = globalId;
        }

        public DreamValue GetValue() {
            return _globals[GlobalId];
        }

        public void Assign(DreamValue value) {
            _globals[GlobalId] = value;
        }
    }

    struct DreamProcIdentifierLocalVariable : IDreamProcIdentifier {
        private DreamValue[] _localVariables;

        public int ID;

        public DreamProcIdentifierLocalVariable(DreamValue[] localVariables, int id) {
            _localVariables = localVariables;
            ID = id;
        }

        public DreamValue GetValue() {
            return _localVariables[ID];
        }

        public void Assign(DreamValue value) {
            _localVariables[ID] = value;
        }
    }

    struct DreamProcIdentifierProc : IDreamProcIdentifier {
        public DreamProc Proc;
        public DreamObject Instance;

        public DreamProcIdentifierProc(DreamProc proc, DreamObject instance) {
            Proc = proc;
            Instance = instance;
        }

        public DreamValue GetValue() {
            return new DreamValue(Proc);
        }

        public void Assign(DreamValue value) {
            throw new Exception("Cannot assign to a proc");
        }
    }

    struct DreamProcIdentifierIndex : IDreamProcIdentifier {
        public DreamObject Object;
        public DreamValue Index;

        public DreamProcIdentifierIndex(DreamObject dreamObject, DreamValue index) {
            Object = dreamObject;
            Index = index;
        }

        public DreamValue GetValue() {
            return Object.ObjectDefinition.MetaObject?.OperatorIndex(Object, Index) ?? DreamValue.Null;
        }

        public void Assign(DreamValue value) {
            Object.ObjectDefinition.MetaObject?.OperatorIndexAssign(Object, Index, value);
        }
    }

    struct DreamProcIdentifierSelfProc : IDreamProcIdentifier {
        public ProcState State;

        public DreamProcIdentifierSelfProc(ProcState state) {
            State = state;
        }

        public DreamValue GetValue() {
            return State.Result;
        }

        public void Assign(DreamValue value) {
            State.Result = value;
        }
    }

    struct DreamProcIdentifierNull : IDreamProcIdentifier {
        public DreamValue GetValue() {
            return DreamValue.Null;
        }

        public void Assign(DreamValue value) {
            throw new InvalidOperationException();
        }
    }
}
