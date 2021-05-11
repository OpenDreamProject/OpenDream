using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Objects {
    class DreamObject {
        public DreamObjectDefinition ObjectDefinition;
        public bool Deleted = false;

        /// <summary>
        /// Any variables that may differ from the default
        /// </summary>
        private Dictionary<string, DreamValue> _variables = new();

        private static readonly Dictionary<DreamObject, int> _referenceIDs = new();

        public DreamObject(DreamObjectDefinition objectDefinition, DreamProcArguments creationArguments) {
            ObjectDefinition = objectDefinition;

            ObjectDefinition.InitializionProc?.Run(this, new DreamProcArguments(new(), new()));

            ObjectDefinition.MetaObject?.OnObjectCreated(this, creationArguments);
        }

        ~DreamObject() {
            Delete();
        }

        public static DreamObject GetFromReferenceID(int refID) {
            foreach (KeyValuePair<DreamObject, int> referenceIDPair in _referenceIDs) {
                if (referenceIDPair.Value == refID) return referenceIDPair.Key;
            }

            return null;
        }

        public int CreateReferenceID() {
            int referenceID;

            if (!_referenceIDs.TryGetValue(this, out referenceID)) {
                referenceID = _referenceIDs.Count;

                _referenceIDs.Add(this, referenceID);
            }

            return referenceID;
        }

        public void Delete() {
            if (Deleted) return;
            ObjectDefinition.MetaObject?.OnObjectDeleted(this);

            _referenceIDs.Remove(this);
            Deleted = true;
        }

        public void CopyFrom(DreamObject from) {
            ObjectDefinition = from.ObjectDefinition;
            _variables = from._variables;
        }

        public bool IsSubtypeOf(DreamPath path) {
            return ObjectDefinition.IsSubtypeOf(path);
        }

        public bool HasVariable(string name) {
            return ObjectDefinition.HasVariable(name); ;
        }

        public DreamValue GetVariable(string name) {
            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new Exception("Variable " + name + " doesn't exist");
            }
        }

        public bool TryGetVariable(string name, out DreamValue variableValue) {
            if (_variables.TryGetValue(name, out variableValue) || ObjectDefinition.Variables.TryGetValue(name, out variableValue)) {
                if (ObjectDefinition.MetaObject != null) variableValue = ObjectDefinition.MetaObject.OnVariableGet(this, name, variableValue);

                return true;
            }

            return false;
        }

        public void SetVariable(string name, DreamValue value) {
            DreamValue oldValue = _variables.ContainsKey(name) ? _variables[name] : ObjectDefinition.Variables[name];

            _variables[name] = value;
            if (ObjectDefinition.MetaObject != null) ObjectDefinition.MetaObject.OnVariableSet(this, name, value, oldValue);
        }

        public DreamProc GetProc(string procName) {
            return ObjectDefinition.GetProc(procName);
        }

        public bool TryGetProc(string procName, out DreamProc proc) {
            return ObjectDefinition.TryGetProc(procName, out proc);
        }

        public DreamValue CallProc(string procName, DreamProcArguments arguments, DreamObject usr = null) {
            try {
                DreamProc proc = GetProc(procName);

                return proc.Run(this, arguments, usr);
            } catch (Exception e) {
                Console.WriteLine("Exception while running proc '" + procName + "' on object of type '" + ObjectDefinition.Type + "': " + e.Message);
            }

            return DreamValue.Null;
        }

        public DreamValue CallProc(string procName) {
            return CallProc(procName, new DreamProcArguments(null));
        }

        public override string ToString() {
            return "DreamObject(" + ObjectDefinition.Type + ")";
        }
    }
}
