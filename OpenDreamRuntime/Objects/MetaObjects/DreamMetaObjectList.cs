using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectList : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public DreamMetaObjectList() {
            IoCManager.InjectDependencies(this);
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            // Named arguments are ignored
            if (creationArguments.OrderedArguments != null) {
                if (creationArguments.OrderedArguments.Count > 1) { // Multi-dimensional
                    DreamList[] lists = { (DreamList)dreamObject };

                    int dimensions = creationArguments.OrderedArguments.Count;
                    for (int argIndex = 0; argIndex < dimensions; argIndex++) {
                        DreamValue arg = creationArguments.OrderedArguments[argIndex];
                        arg.TryGetValueAsInteger(out int size);

                        DreamList[] newLists = null;
                        if (argIndex < dimensions) {
                            newLists = new DreamList[size * lists.Length];
                        }

                        for (int i = 0; i < lists.Length; i++) {
                            DreamList list = lists[i];

                            for (int j = 0; j < size; j++) {
                                if (argIndex < dimensions - 1) {
                                    DreamList newList = DreamList.Create();

                                    list.AddValue(new DreamValue(newList));
                                    newLists[i * size + j] = newList;
                                } else {
                                    list.AddValue(DreamValue.Null);
                                }
                            }
                        }

                        lists = newLists;
                    }
                } else if (creationArguments.OrderedArguments.Count == 1 && creationArguments.OrderedArguments[0].TryGetValueAsInteger(out int size)) {
                    ((DreamList)dreamObject).Resize(size);
                }
            }
        }

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "len") {
                DreamList list = (DreamList)dreamObject;
                value.TryGetValueAsInteger(out var newLen);

                list.Resize(newLen);
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value) {
            switch (varName) {
                case "len": {
                    DreamList list = (DreamList) dreamObject;

                    return new DreamValue(list.GetLength());
                }
                case "type":
                    return new DreamValue(_objectTree.List);
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public ProcStatus? OperatorAdd(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.AddValue(value);
                }
            } else {
                listCopy.AddValue(b);
            }

            state.Push(new DreamValue(listCopy));
            return null;
        }

        public ProcStatus? OperatorSubtract(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.RemoveValue(value);
                }
            } else {
                listCopy.RemoveValue(b);
            }

            state.Push(new DreamValue(listCopy));
            return null;
        }

        public ProcStatus? OperatorAppend(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    list.AddValue(value);
                }
            } else {
                list.AddValue(b);
            }
            state.Push(new DreamValue(list));
            return null;
        }

        public ProcStatus? OperatorRemove(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                DreamValue[] values = bList.GetValues().ToArray();

                foreach (DreamValue value in values) {
                    list.RemoveValue(value);
                }
            } else {
                list.RemoveValue(b);
            }

            state.Push(a);
            return null;
        }

        public ProcStatus? OperatorOr(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {    // List | List
                list = list.Union(bList);
            } else {                                                // List | x
                list = list.CreateCopy();
                list.AddValue(b);
            }

            state.Push(new DreamValue(list));
            return null;
        }

        public ProcStatus? OperatorEquivalent(DreamValue a, DreamValue b, DMProcState state) {
            if (a.TryGetValueAsDreamList(out var firstList) && b.TryGetValueAsDreamList(out var secondList)) {
                if (firstList.GetLength() != secondList.GetLength())
                {
                    state.Push(DreamValue.False);
                    return null;
                }
                var firstValues = firstList.GetValues();
                var secondValues = secondList.GetValues();
                for (var i = 0; i < firstValues.Count; i++) {
                    if (!firstValues[i].Equals(secondValues[i]))
                    {
                        state.Push(DreamValue.False);
                        return null;
                    }
                }
                state.Push(DreamValue.True);
                return null;
            }
            state.Push(DreamValue.False);
            return null;// This will never be true, because reaching this line means b is not a list, while a will always be.
        }
        public ProcStatus? OperatorNotEquivalent(DreamValue a, DreamValue b, DMProcState state) {
            if (a.TryGetValueAsDreamList(out var firstList) && b.TryGetValueAsDreamList(out var secondList)) {
                if (firstList.GetLength() != secondList.GetLength())
                {
                    state.Push(DreamValue.True);
                    return null;
                }
                var firstValues = firstList.GetValues();
                var secondValues = secondList.GetValues();
                for (var i = 0; i < firstValues.Count; i++) {
                    if (!firstValues[i].Equals(secondValues[i]))
                    {
                        state.Push(DreamValue.True);
                        return null;
                    }
                }
                state.Push(DreamValue.False);
                return null;
            }
            state.Push(DreamValue.True);
            return null;// This will never be true, because reaching this line means b is not a list, while a will always be.
        }
        public ProcStatus? OperatorCombine(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    if (!list.ContainsValue(value)) {
                        list.AddValue(value);
                    }
                }
            } else if (!list.ContainsValue(b)) {
                list.AddValue(b);
            }

            state.Push(a);
            return null;
        }

        public ProcStatus? OperatorMask(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                for (int i = 1; i <= list.GetLength(); i++) {
                    if (!bList.ContainsValue(list.GetValue(new DreamValue(i)))) {
                        list.Cut(i, i + 1);
                        i--;
                    }
                }
            } else {
                for (int i = 1; i <= list.GetLength(); i++) {
                    if (list.GetValue(new DreamValue(i)) != b) {
                        list.Cut(i, i + 1);
                        i--;
                    }
                }
            }

            state.Push(a);
            return null;
        }

        public ProcStatus? OperatorBitAnd(DreamValue a, DreamValue b, DMProcState state)
        {
            if (a.TryGetValueAsDreamList(out DreamList list)) {
                DreamList newList = DreamList.Create();
                if (b.TryGetValueAsDreamList(out DreamList secondList)) {
                    int len = list.GetLength();
                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (secondList.ContainsValue(value)) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue != DreamValue.Null) newList.SetValue(value, associativeValue);
                        }
                    }
                } else {
                    int len = list.GetLength();

                    for (int i = 1; i <= len; i++) {
                        DreamValue value = list.GetValue(new DreamValue(i));

                        if (value == b) {
                            DreamValue associativeValue = list.GetValue(value);

                            newList.AddValue(value);
                            if (associativeValue != DreamValue.Null) newList.SetValue(value, associativeValue);
                        }
                    }
                }

                state.Push(new DreamValue(newList));
            }
            else
            {
                state.Push(new DreamValue(0));
            }
            return null;
        }

        public ProcStatus? OperatorBitXor(DreamValue a, DreamValue b, DMProcState state)
        {
            DreamList list = a.MustGetValueAsDreamList();
            DreamList newList = DreamList.Create();
            List<DreamValue> values;

            if (b.TryGetValueAsDreamList(out DreamList secondList)) {
                values = secondList.GetValues();
            } else {
                values = new List<DreamValue>() { b };
            }

            foreach (DreamValue value in values) {
                bool inFirstList = list.ContainsValue(value);
                bool inSecondList = secondList.ContainsValue(value);

                if (inFirstList ^ inSecondList) {
                    newList.AddValue(value);

                    DreamValue associatedValue = inFirstList ? list.GetValue(value) : secondList.GetValue(value);
                    if (associatedValue != DreamValue.Null) newList.SetValue(value, associatedValue);
                }
            }

            state.Push(new DreamValue(newList));
            return null;
        }
        public ProcStatus? OperatorIndex(DreamValue a, DreamValue index, DMProcState state) {
            DreamList dreamList = a.MustGetValueAsDreamList();
            state.Push(dreamList.GetValue(index));
            return null;
        }

        public ProcStatus? OperatorIndexAssign(DreamValue a, DreamValue index, DreamValue value, DMProcState state) {
            DreamList dreamList = a.MustGetValueAsDreamList();
            dreamList.SetValue(index, value);
            state.Push(value);
            return null;
        }
    }
}
