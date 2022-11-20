using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs {
    public readonly struct DreamProcArguments {
        public readonly List<DreamValue> OrderedArguments;
        public readonly Dictionary<string, DreamValue> NamedArguments;

        public int ArgumentCount => OrderedArguments.Count + NamedArguments.Count;

        public DreamProcArguments() : this(null, null) {}

        public DreamProcArguments(List<DreamValue>? orderedArguments = null, Dictionary<string, DreamValue>? namedArguments = null) {
            OrderedArguments = orderedArguments ?? new List<DreamValue>();
            NamedArguments = namedArguments ?? new Dictionary<string, DreamValue>();
        }

        public List<DreamValue> GetAllArguments() {
            List<DreamValue> allArguments = new List<DreamValue>();

            allArguments.AddRange(OrderedArguments);
            allArguments.AddRange(NamedArguments.Values);
            return allArguments;
        }

        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                return argumentValue;
            }
            if (OrderedArguments.Count > argumentPosition) {
                return OrderedArguments[argumentPosition];
            }
            return DreamValue.Null;
        }
    }
}
