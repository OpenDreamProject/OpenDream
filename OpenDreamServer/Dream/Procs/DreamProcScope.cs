using OpenDreamServer.Dream.Objects;
using System;
using System.Collections.Generic;

namespace OpenDreamServer.Dream.Procs {
    class DreamProcScope {
        public DreamProcScope ParentScope;
        public DreamObject DreamObject;
        public DreamObject Usr;

        private Dictionary<string, DreamValue> Variables = new Dictionary<string, DreamValue>();

        public DreamProcScope(DreamObject dreamObject, DreamObject usr) {
            ParentScope = null;
            DreamObject = dreamObject;
            Usr = usr;
        }

        public DreamProcScope(DreamProcScope parentScope) {
            ParentScope = parentScope;
            DreamObject = parentScope.DreamObject;
            Usr = parentScope.Usr;
        }

        public DreamValue GetValue(string valueName) {
            if (Variables.ContainsKey(valueName)) {
                return Variables[valueName];
            } else if (ParentScope != null) {
                return ParentScope.GetValue(valueName);
            } else if (DreamObject != null && DreamObject.HasVariable(valueName)) {
                return DreamObject.GetVariable(valueName);
            } else if (DreamObject != null && DreamObject.ObjectDefinition.HasGlobalVariable(valueName)) {
                return DreamObject.ObjectDefinition.GetGlobalVariable(valueName).Value;
            } else if (DreamObject != null && DreamObject.HasProc(valueName)) {
                return new DreamValue(DreamObject.GetProc(valueName));
            } else {
                throw new Exception("Value '" + valueName + "' doesn't exist");
            }
        }

        public void AssignValue(string valueName, DreamValue value) {
            if (Variables.ContainsKey(valueName)) {
                Variables[valueName] = value;
            } else if (ParentScope != null) {
                ParentScope.AssignValue(valueName, value);
            } else if (DreamObject != null && DreamObject.HasVariable(valueName)) {
                DreamObject.SetVariable(valueName, value);
            } else if (DreamObject != null && DreamObject.ObjectDefinition.HasGlobalVariable(valueName)) {
                DreamObject.ObjectDefinition.GetGlobalVariable(valueName).Value = value;
            } else {
                throw new Exception("Value '" + valueName + "' doesn't exist");
            }
        }

        public void CreateVariable(string name, DreamValue value) {
            Variables.Add(name, value);
        }
    }
}
