using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamRuntime.Objects {

    public class DreamObject {
        public DreamRuntime Runtime { get; }

        public DreamObjectDefinition ObjectDefinition;
        public bool Deleted = false;

        /// <summary>
        /// Any variables that may differ from the default
        /// </summary>
        private Dictionary<string, DreamValue> _variables = new();

        public DreamObject(DreamRuntime runtime, DreamObjectDefinition objectDefinition) {
            Runtime = runtime;
            ObjectDefinition = objectDefinition;
        }

        public void InitSpawn(DreamProcArguments creationArguments) {
            var thread = new DreamThread(Runtime);
            var procState = InitProc(thread, null, creationArguments);
            thread.PushProcState(procState);

            if (thread.Resume() == DreamValue.Null) {
                throw new InvalidOperationException($"DreamObject.InitSpawn ({this.ObjectDefinition.Type}) called a yielding proc!");
            }
        }

        public ProcState InitProc(DreamThread thread, DreamObject usr, DreamProcArguments arguments) {
            return new InitDreamObjectState(thread, this, usr, arguments);
        }

        public static DreamObject GetFromReferenceID(DreamRuntime runtime, int refID) {
            foreach (KeyValuePair<DreamObject, int> referenceIDPair in runtime.ReferenceIDs) {
                if (referenceIDPair.Value == refID) return referenceIDPair.Key;
            }

            return null;
        }

        public int CreateReferenceID() {
            int referenceID;

            if (!Runtime.ReferenceIDs.TryGetValue(this, out referenceID)) {
                referenceID = Runtime.ReferenceIDs.Count;

                Runtime.ReferenceIDs.Add(this, referenceID);
            }

            return referenceID;
        }

        public void Delete() {
            if (Deleted) return;
            ObjectDefinition.MetaObject?.OnObjectDeleted(this);

            Runtime.ReferenceIDs.Remove(this);
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
            return ObjectDefinition.HasVariable(name);
        }

        public DreamValue GetVariable(string name) {
            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new Exception("Variable " + name + " doesn't exist");
            }
        }

        public List<DreamValue> GetVariableNames() {
            List<DreamValue> list = new(_variables.Count);
            foreach (String key in _variables.Keys) {
                list.Add(new(key));
            }
            return list;
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

        public DreamValue SpawnProc(string procName, DreamProcArguments arguments, DreamObject usr = null) {
            var proc = GetProc(procName);
            return DreamThread.Run(proc, this, usr, arguments);
        }

        public DreamValue SpawnProc(string procName) {
            return SpawnProc(procName, new DreamProcArguments(null));
        }

        public override string ToString() {
            return "DreamObject(" + ObjectDefinition.Type + ")";
        }
    }
}
