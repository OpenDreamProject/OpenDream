namespace OpenDreamRuntime.Procs {
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class DreamProcAttribute : Attribute {
        public string Name;

        public DreamProcAttribute(string name) {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DreamProcParameterAttribute : Attribute {
        public string Name;
        public DreamValue.DreamValueTypeFlag Type;
        public object? DefaultValue;

        public DreamProcParameterAttribute(string name) {
            Name = name;
        }
    }
}
