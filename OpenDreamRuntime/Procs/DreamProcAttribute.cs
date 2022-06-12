namespace OpenDreamRuntime.Procs {
    [AttributeUsage(AttributeTargets.Method)]
    sealed class DreamProcAttribute : Attribute {
        public string Name;

        public DreamProcAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class DreamProcParameterAttribute : Attribute {
        public string Name;
        public DreamValue.DreamValueType Type;
        public object DefaultValue;

        public DreamProcParameterAttribute(string name) {
            Name = name;
        }
    }
}
