using System;

namespace OpenDreamVM.Procs {
    [AttributeUsage(AttributeTargets.Method)]
    class DreamProcAttribute : Attribute {
        public string Name;

        public DreamProcAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class DreamProcParameterAttribute : Attribute {
        public string Name;
        public DreamValue.DreamValueType Type;
        public object DefaultValue;

        public DreamProcParameterAttribute(string name) {
            Name = name;
        }
    }
}
