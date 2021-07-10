using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace OpenDreamRuntime.Objects {
    public class DreamObjectDefinition {
        public DreamRuntime Runtime { get; }
        public DreamPath Type;
        public IDreamMetaObject MetaObject = null;
        public DreamProc InitializionProc = null;
        public Dictionary<string, DreamProc> Procs { get; private set; } = new();
        public Dictionary<string, DreamProc> OverridingProcs { get; private set; } = new();
        public Dictionary<string, DreamValue> Variables { get; private set; } = new();
        public Dictionary<string, DreamGlobalVariable> GlobalVariables { get; private set; } = new();

        private DreamObjectDefinition _parentObjectDefinition = null;

        public DreamObjectDefinition(DreamRuntime runtime, DreamPath type) {
            Runtime = runtime;
            Type = type;
        }

        public DreamObjectDefinition(DreamObjectDefinition copyFrom) {
            Runtime = copyFrom.Runtime;
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

            Runtime = parentObjectDefinition.Runtime;
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

        private (string, Dictionary<string, DreamValue>, List<String>) GetNativeInfo(Delegate func) {
            List<Attribute> attributes = new(func.GetInvocationList()[0].Method.GetCustomAttributes());
            DreamProcAttribute procAttribute = (DreamProcAttribute)attributes.Find(attribute => attribute is DreamProcAttribute);
            if (procAttribute == null) throw new ArgumentException();

            Dictionary<string, DreamValue> defaultArgumentValues = null;
            var argumentNames = new List<string>();
            List<Attribute> parameterAttributes = attributes.FindAll(attribute => attribute is DreamProcParameterAttribute);
            foreach (Attribute attribute in parameterAttributes) {
                DreamProcParameterAttribute parameterAttribute = (DreamProcParameterAttribute)attribute;

                argumentNames.Add(parameterAttribute.Name);
                if (parameterAttribute.DefaultValue != default) {
                    if (defaultArgumentValues == null) defaultArgumentValues = new Dictionary<string, DreamValue>();

                    defaultArgumentValues.Add(parameterAttribute.Name, new DreamValue(parameterAttribute.DefaultValue));
                }
            }

            return (procAttribute.Name, defaultArgumentValues, argumentNames);
        }

        public void SetNativeProc(NativeProc.HandlerFn func) {
            var (name, defaultArgumentValues, argumentNames) = GetNativeInfo(func);
            var proc = new NativeProc(name, Runtime, null, argumentNames, null, defaultArgumentValues, func);
            SetProcDefinition(name, proc);
        }

        public void SetNativeProc(Func<AsyncNativeProc.State, Task<DreamValue>> func) {
            var (name, defaultArgumentValues, argumentNames) = GetNativeInfo(func);
            var proc = new AsyncNativeProc(name, Runtime, null, argumentNames, null, defaultArgumentValues, func);
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

        private void CopyVariablesFrom(DreamObjectDefinition definition)
        {
            Variables = new Dictionary<string, DreamValue>(definition.Variables.Count);
            foreach (KeyValuePair<string, DreamValue> variable in definition.Variables) {
                Variables.Add(variable.Key, variable.Value);
            }

            GlobalVariables = new Dictionary<string, DreamGlobalVariable>(definition.GlobalVariables.Count);
            foreach (KeyValuePair<string, DreamGlobalVariable> globalVariable in definition.GlobalVariables) {
                GlobalVariables.Add(globalVariable.Key, globalVariable.Value);
            }
        }
    }
}
