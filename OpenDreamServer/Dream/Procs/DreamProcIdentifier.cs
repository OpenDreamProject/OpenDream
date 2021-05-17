using OpenDreamServer.Dream.Objects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Procs {
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
            } else if (Instance.ObjectDefinition.HasGlobalVariable(IdentifierName)) {
                return Instance.ObjectDefinition.GetGlobalVariable(IdentifierName).Value;
            } else {
                throw new Exception("Value '" + IdentifierName + "' doesn't exist");
            }
        }

        public void Assign(DreamValue value) {
            if (Instance.HasVariable(IdentifierName)) {
                Instance.SetVariable(IdentifierName, value);
            } else if (Instance.ObjectDefinition.HasGlobalVariable(IdentifierName)) {
                Instance.ObjectDefinition.GetGlobalVariable(IdentifierName).Value = value;
            } else {
                throw new Exception("Value '" + IdentifierName + "' doesn't exist");
            }
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

    struct DreamProcIdentifierListIndex : IDreamProcIdentifier {
        public DreamList List;
        public DreamValue ListIndex;

        public DreamProcIdentifierListIndex(DreamList list, DreamValue listIndex) {
            List = list;
            ListIndex = listIndex;

            if (!list.IsSubtypeOf(DreamPath.List)) {
                throw new ArgumentException("Parameter must be a dream object of type " + DreamPath.List, nameof(list));
            }
        }

        public DreamValue GetValue() {
            return List.GetValue(ListIndex);
        }

        public void Assign(DreamValue value) {
            List.SetValue(ListIndex, value);
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
}
