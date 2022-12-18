using System.Linq;

namespace OpenDreamRuntime.Procs {
    public readonly struct DreamProcArguments {
        public readonly List<DreamValue>? OrderedArguments;
        public readonly Dictionary<string, DreamValue>? NamedArguments;

        public int OrderedArgumentCount => OrderedArguments?.Count ?? 0;
        public int NamedArgumentCount => NamedArguments?.Count ?? 0;
        public int ArgumentCount => OrderedArgumentCount + NamedArgumentCount;

        public IEnumerable<DreamValue> IterOrderedArguments => OrderedArguments ?? Enumerable.Empty<DreamValue>();

        public DreamProcArguments(List<DreamValue>? orderedArguments = null, Dictionary<string, DreamValue>? namedArguments = null) {
            OrderedArguments = orderedArguments;
            NamedArguments = namedArguments;
        }

        public List<DreamValue> GetAllArguments() {
            List<DreamValue> allArguments = new List<DreamValue>();
            if (OrderedArguments != null)
                allArguments.AddRange(OrderedArguments);
            if (NamedArguments != null)
                allArguments.AddRange(NamedArguments.Values);
            return allArguments;
        }

        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (NamedArguments != null && NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                return argumentValue;
            }
            if (OrderedArguments != null && OrderedArguments.Count > argumentPosition) {
                return OrderedArguments[argumentPosition];
            }
            return DreamValue.Null;
        }

        public bool TryGetNamedArgument(string argumentName, [NotNullWhen(true)] out DreamValue? value) {
            if(NamedArguments == null) {
                value = DreamValue.Null;
                return false;
            }

            if(NamedArguments.TryGetValue(argumentName, out DreamValue lookup)) {
                value = lookup;
                return true;
            }

            value = DreamValue.Null;
            return false;
        }

        public bool TryGetPositionalArgument(int index, [NotNullWhen(true)] out DreamValue? value) {
            if(OrderedArguments == null) {
                value = DreamValue.Null;
                return false;
            }

            if(OrderedArguments.TryGetValue(index, out DreamValue lookup)) {
                value = lookup;
                return true;
            }

            value = DreamValue.Null;
            return false;
        }
    }
}
