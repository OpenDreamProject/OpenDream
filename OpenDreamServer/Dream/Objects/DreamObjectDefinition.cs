using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenDreamServer.Dream.Objects {
    class DreamObjectDefinition {
        public DreamPath Type;
        public IDreamMetaObject MetaObject = null;
        public DreamProc InitializionProc = null;
        public Dictionary<string, DreamProc> Procs { get; private set; } = new();
        public Dictionary<string, DreamProc> OverridingProcs { get; private set; } = new();
        public Dictionary<string, DreamValue> Variables { get; private set; } = new();
        public Dictionary<string, DreamGlobalVariable> GlobalVariables { get; private set; } = new();

        private DreamObjectDefinition _parentObjectDefinition = null;

        public DreamObjectDefinition(DreamPath type) {
            Type = type;
        }

        public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
            Type = copyFrom.Type;
            MetaObject = copyFrom.MetaObject;
            InitializionProc = copyFrom.InitializionProc;
            _parentObjectDefinition = copyFrom._parentObjectDefinition;

            CopyVariablesFrom(copyFrom);

            foreach (KeyValuePair<string, DreamProc> proc in copyFrom.Procs) {
                Procs.Add(proc.Key, proc.Value);
            }

            foreach (KeyValuePair<string, DreamProc> proc in copyFrom.OverridingProcs) {
                OverridingProcs.Add(proc.Key, proc.Value);
            }
        }

        public DreamObjectDefinition(DreamPath type, DreamObjectDefinition parentObjectDefinition) {
            CopyVariablesFrom(parentObjectDefinition);

            Type = type;
            InitializionProc = parentObjectDefinition.InitializionProc;
            _parentObjectDefinition = parentObjectDefinition;
        }

        public void SetVariableDefinition(string variableName, DreamValue value) {
            Variables[variableName] = value;
        }

        public void SetProcDefinition(string procName, DreamProc proc) {
            if (HasProc(procName)) {
                proc.SuperProc = GetProc(procName);
                OverridingProcs[procName] = proc;
            } else {
                Procs[procName] = proc;
            }
        }

        public void SetNativeProc(Func<DreamObject, DreamObject, DreamProcArguments, DreamValue> nativeProc) {
            List<Attribute> attributes = new(nativeProc.GetInvocationList()[0].Method.GetCustomAttributes());
            DreamProcAttribute procAttribute = (DreamProcAttribute)attributes.Find(attribute => attribute is DreamProcAttribute);
            if (procAttribute == null) throw new ArgumentException();

            Procs[procAttribute.Name] = new DreamProc(nativeProc);
        }

        public DreamProc GetProc(string procName) {
            if (TryGetProc(procName, out DreamProc proc)) {
                return proc;
            } else {
                throw new Exception("Object type '" + Type + "' does not have a proc named '" + procName + "'");
            }
        }

        public bool TryGetProc(string procName, out DreamProc proc) {
            if (OverridingProcs.TryGetValue(procName, out proc)) {
                return true;
            } else if (Procs.TryGetValue(procName, out proc)) {
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.TryGetProc(procName, out proc);
            } else {
                return false;
            }
        }

        public bool HasProc(string procName) {
            if (Procs.ContainsKey(procName)) {
                return true;
            } else if (_parentObjectDefinition != null) {
                return _parentObjectDefinition.HasProc(procName);
            } else {
                return false;
            }
        }

        public bool HasVariable(string variableName) {
            return Variables.ContainsKey(variableName);
        }

        public bool HasGlobalVariable(string globalVariableName) {
            return GlobalVariables.ContainsKey(globalVariableName);
        }

        public DreamGlobalVariable GetGlobalVariable(string globalVariableName) {
            if (!HasGlobalVariable(globalVariableName)) {
                throw new Exception("Object type '" + Type + "' does not have a global variable named '" + globalVariableName + "'");
            }

            return GlobalVariables[globalVariableName];
        }

        public bool IsSubtypeOf(DreamPath path) {
            if (Type.IsDescendantOf(path)) return true;
            else if (_parentObjectDefinition != null) return _parentObjectDefinition.IsSubtypeOf(path);
            else return false;
        }

        private void CopyVariablesFrom(DreamObjectDefinition definition) {
            foreach (KeyValuePair<string, DreamValue> variable in definition.Variables) {
                Variables.Add(variable.Key, variable.Value);
            }

            foreach (KeyValuePair<string, DreamGlobalVariable> globalVariable in definition.GlobalVariables) {
                GlobalVariables.Add(globalVariable.Key, globalVariable.Value);
            }
        }
    }
}
