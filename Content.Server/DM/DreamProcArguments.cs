using Content.Server.Dream;
using System.Collections.Generic;

namespace Content.Server.DM {
    public struct DreamProcArguments {
        public List<DreamValue> OrderedArguments;
        public Dictionary<string, DreamValue> NamedArguments;

        public int ArgumentCount => OrderedArguments.Count + NamedArguments.Count;

        public DreamProcArguments(List<DreamValue> orderedArguments, Dictionary<string, DreamValue> namedArguments = null) {
            OrderedArguments = orderedArguments ?? new List<DreamValue>();
            NamedArguments = namedArguments ?? new Dictionary<string, DreamValue>();
        }

        public List<DreamValue> GetAllArguments() {
            List<DreamValue> AllArguments = new List<DreamValue>();

            AllArguments.AddRange(OrderedArguments);
            AllArguments.AddRange(NamedArguments.Values);
            return AllArguments;
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
