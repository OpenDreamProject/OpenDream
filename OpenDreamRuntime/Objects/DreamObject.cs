using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects {
    [Virtual]
    public class DreamObject {
        public DreamObjectDefinition? ObjectDefinition { get; protected set; }
        public bool Deleted = false;

        private Dictionary<string, DreamValue> _variables = new();

        public DreamObject(DreamObjectDefinition? objectDefinition) {
            ObjectDefinition = objectDefinition;
        }

        public void InitSpawn(DreamProcArguments creationArguments) {
            var thread = new DreamThread();
            var procState = InitProc(thread, null, creationArguments);
            thread.PushProcState(procState);

            if (thread.Resume() == DreamValue.Null) {
                thread.HandleException(new InvalidOperationException("DreamObject.InitSpawn called a yielding proc!"));
            }
        }

        public ProcState InitProc(DreamThread thread, DreamObject usr, DreamProcArguments arguments) {
            if(Deleted){
                throw new Exception("Cannot init proc on a deleted object");
            }
            return new InitDreamObjectState(thread, this, usr, arguments);
        }

        public static DreamObject GetFromReferenceID(IDreamManager manager, int refID) {
            foreach (KeyValuePair<DreamObject, int> referenceIDPair in manager.ReferenceIDs) {
                if (referenceIDPair.Value == refID) return referenceIDPair.Key;
            }

            return null;
        }

        public int CreateReferenceID(IDreamManager manager) {
            if(Deleted){
                throw new Exception("Cannot create reference ID for an object that is deleted"); // i dont believe this will **ever** be called, but just to be sure, funky errors /might/ appear in the future if someone does a fucky wucky and calls this on a deleted object.
            }
            int referenceID;

            if (!manager.ReferenceIDs.TryGetValue(this, out referenceID)) {
                referenceID = manager.ReferenceIDs.Count;

                manager.ReferenceIDs.Add(this, referenceID);
            }

            return referenceID;
        }

        public void Delete(IDreamManager manager) {
            if (Deleted) return;
            ObjectDefinition?.MetaObject?.OnObjectDeleted(this);
            Deleted = true;
            //we release all relevant information, making this a very tiny object
            _variables = null;
            ObjectDefinition = null;

            manager.ReferenceIDs.Remove(this);
        }

        public void CopyFrom(DreamObject from) {
            ObjectDefinition = from.ObjectDefinition;
            _variables = from._variables;
        }

        public bool IsSubtypeOf(DreamPath path) {
            return ObjectDefinition.IsSubtypeOf(path);
        }

        public bool HasVariable(string name) {
            if(Deleted){
                return false;
            }
            return ObjectDefinition.HasVariable(name);
        }

        public DreamValue GetVariable(string name) {
            if(Deleted){
                throw new Exception("Cannot read " + name + " on a deleted object");
            }
            if (TryGetVariable(name, out DreamValue variableValue)) {
                return variableValue;
            } else {
                throw new Exception("Variable " + name + " doesn't exist");
            }
        }

        public List<DreamValue> GetVariableNames() {
            if(Deleted){
                throw new Exception("Cannot get variable names of a deleted object");
            }
            List<DreamValue> list = new(_variables.Count);
            foreach (String key in _variables.Keys) {
                list.Add(new(key));
            }
            return list;
        }

        public bool TryGetVariable(string name, out DreamValue variableValue) {
            if(Deleted){
                throw new Exception("Cannot try to get variable on a deleted object");
            }
            if (_variables.TryGetValue(name, out variableValue) || ObjectDefinition.Variables.TryGetValue(name, out variableValue)) {
                if (ObjectDefinition.MetaObject != null) variableValue = ObjectDefinition.MetaObject.OnVariableGet(this, name, variableValue);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Handles setting a variable, and special behavior by calling OnVariableSet()
        /// </summary>
        public void SetVariable(string name, DreamValue value) {
            if(Deleted){
                throw new Exception("Cannot set variable on a deleted object!");
            }
            var oldValue = SetVariableValue(name, value);
            if (ObjectDefinition.MetaObject != null) ObjectDefinition.MetaObject.OnVariableSet(this, name, value, oldValue);
        }

        /// <summary>
        /// Directly sets a variable's value, bypassing any special behavior
        /// </summary>
        /// <returns>The OLD variable value</returns>
        public DreamValue SetVariableValue(string name, DreamValue value) {
            if(Deleted){
                throw new Exception("Cannot set variable on a deleted object");
            }
            DreamValue oldValue = _variables.ContainsKey(name) ? _variables[name] : ObjectDefinition.Variables[name];
            _variables[name] = value;
            return oldValue;
        }

        public DreamProc GetProc(string procName) {
            if(Deleted){
                throw new Exception("Cannot get proc on a deleted object");
            }
            return ObjectDefinition.GetProc(procName);
        }

        public bool TryGetProc(string procName, out DreamProc proc) {
            if(Deleted){
                throw new Exception("Cannot try to get proc on a deleted object");
            }
            return ObjectDefinition.TryGetProc(procName, out proc);
        }

        public DreamValue SpawnProc(string procName, DreamProcArguments arguments, DreamObject usr = null) {
            if(Deleted){
                throw new Exception("Cannot spawn proc on a deleted object");
            }
            var proc = GetProc(procName);
            return DreamThread.Run(proc, this, usr, arguments);
        }

        public DreamValue SpawnProc(string procName) {
            return SpawnProc(procName, new DreamProcArguments(null));
        }

        public override string ToString() {
            if(Deleted){
                return "DreamObject(DELETED)";
            }
            return "DreamObject(" + ObjectDefinition.Type + ")";
        }
    }
}
