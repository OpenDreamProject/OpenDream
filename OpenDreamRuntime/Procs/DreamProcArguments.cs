using OpenDreamRuntime.Objects;

namespace OpenDreamRuntime.Procs {
    public readonly struct DreamProcArguments {
        public readonly List<DreamValue> OrderedArguments;
        public readonly Dictionary<string, DreamValue> NamedArguments;

        public int ArgumentCount => OrderedArguments.Count + NamedArguments.Count;

        public DreamProcArguments(List<DreamValue>? orderedArguments, Dictionary<string, DreamValue>? namedArguments = null) {
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

        public DreamList CreateDreamList() {
            DreamList list = DreamList.Create();

            foreach (DreamValue argument in OrderedArguments) {
                list.AddValue(argument);
            }

            foreach (KeyValuePair<string, DreamValue> argument in NamedArguments) {
                list.SetValue(new DreamValue(argument.Key), argument.Value);
            }

            return list;
        }
    }
}
