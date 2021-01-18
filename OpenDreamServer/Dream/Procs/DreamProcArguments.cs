using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDreamServer.Dream.Procs {
    struct DreamProcArguments {
        public List<DreamValue> OrderedArguments;
        public Dictionary<string, DreamValue> NamedArguments;

        public int ArgumentCount {
            get => OrderedArguments.Count + NamedArguments.Count;
        }

        public DreamProcArguments(List<DreamValue> orderedArguments, Dictionary<string, DreamValue> namedArguments = null) {
            OrderedArguments = (orderedArguments != null) ? orderedArguments : new List<DreamValue>();
            NamedArguments = (namedArguments != null) ? namedArguments : new Dictionary<string, DreamValue>();
        }

        public List<DreamValue> GetAllArguments() {
            List<DreamValue> AllArguments = new List<DreamValue>();

            AllArguments.AddRange(OrderedArguments);
            AllArguments.AddRange(NamedArguments.Values);
            return AllArguments;
        }

        public DreamValue GetArgument(int argumentPosition, string argumentName) {
            if (NamedArguments.ContainsKey(argumentName)) {
                return NamedArguments[argumentName];
            } else if (OrderedArguments.Count > argumentPosition) {
                return OrderedArguments[argumentPosition];
            } else {
                throw new Exception("No argument named '" + argumentName + "' or argument at position " + argumentPosition);
            }
        }

        public DreamList CreateDreamList() {
            DreamList list = Program.DreamObjectTree.CreateList();

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
