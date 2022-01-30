using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects {
    public class DreamObjectDefinition {
        public DreamPath Type;
        public IDreamMetaObject MetaObject = null;
        public DreamProc InitializionProc = null;
        public readonly Dictionary<string, DreamProc> Procs = new();
        public readonly Dictionary<string, DreamProc> OverridingProcs = new();
        public readonly Dictionary<string, DreamValue> Variables = new();
        public readonly Dictionary<string, int> GlobalVariables = new();

        private DreamObjectDefinition _parentObjectDefinition = null;

        public DreamObjectDefinition(DreamPath type) {
            Type = type;
        }

        public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
            Type = copyFrom.Type;
            MetaObject = copyFrom.MetaObject;
            InitializionProc = copyFrom.InitializionProc;
            _parentObjectDefinition = copyFrom._parentObjectDefinition;

            Variables = new Dictionary<string, DreamValue>(copyFrom.Variables);
            GlobalVariables = new Dictionary<string, int>(copyFrom.GlobalVariables);
            Procs = new Dictionary<string, DreamProc>(copyFrom.Procs);
            OverridingProcs = new Dictionary<string, DreamProc>(copyFrom.OverridingProcs);
        }

        public DreamObjectDefinition(DreamPath type, DreamObjectDefinition parentObjectDefinition) {
            Type = type;
            InitializionProc = parentObjectDefinition.InitializionProc;
            _parentObjectDefinition = parentObjectDefinition;

            Variables = new Dictionary<string, DreamValue>(parentObjectDefinition.Variables);
            GlobalVariables = new Dictionary<string, int>(parentObjectDefinition.GlobalVariables);
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

        public void SetNativeProc(NativeProc.HandlerFn func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new NativeProc(name, null, argumentNames, null, defaultArgumentValues, func);

            SetProcDefinition(name, proc);
        }

        public void SetNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var (name, defaultArgumentValues, argumentNames) = NativeProc.GetNativeInfo(func);
            var proc = new AsyncNativeProc(name, null, argumentNames, null, defaultArgumentValues, func);

            SetProcDefinition(name, proc);
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

        public bool IsSubtypeOf(DreamPath path) {
            if (Type.IsDescendantOf(path)) return true;
            else if (_parentObjectDefinition != null) return _parentObjectDefinition.IsSubtypeOf(path);
            else return false;
        }
    }
}
