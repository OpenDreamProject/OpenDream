using Robust.Shared.Utility;
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
            List<DreamValue> allArguments = new List<DreamValue>(ArgumentCount);
            if (OrderedArguments != null)
                allArguments.AddRange(OrderedArguments);
            if (NamedArguments != null)
                allArguments.AddRange(NamedArguments.Values);
            return allArguments;
        }

        public IEnumerator<DreamValue> AllArgumentsEnumerator() {
            if (OrderedArguments != null) {
                foreach (DreamValue arg in OrderedArguments) {
                    yield return arg;
                }
            }

            if (NamedArguments != null) {
                foreach (DreamValue arg in NamedArguments.Values) {
                    yield return arg;
                }
            }
        }

        /// <remarks>
        /// Argument position is 0-indexed.
        /// </remarks>
        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (NamedArguments != null && NamedArguments.TryGetValue(argumentName, out DreamValue argumentValue)) {
                return argumentValue;
            }
            if (OrderedArguments != null && OrderedArguments.Count > argumentPosition) {
                return OrderedArguments[argumentPosition];
            }
            return DreamValue.Null;
        }

        public bool TryGetNamedArgument(string argumentName, out DreamValue value) {
            if(NamedArguments == null) {
                value = DreamValue.Null;
                return false;
            }

            return NamedArguments.TryGetValue(argumentName, out value);
        }

        public bool TryGetPositionalArgument(int index, out DreamValue value) {
            if(OrderedArguments == null) {
                value = DreamValue.Null;
                return false;
            }

            return OrderedArguments.TryGetValue(index, out value);
        }

        public override string ToString() {
            return $"<Arguments {OrderedArgumentCount} {NamedArgumentCount}>";
        }
    }
}
