using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectList : IDreamMetaObject {
        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            ParentType?.OnObjectCreated(dreamObject, creationArguments);

            // Named arguments are ignored
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

        public void OnVariableSet(DreamObject dreamObject, string varName, DreamValue value, DreamValue oldValue) {
            ParentType?.OnVariableSet(dreamObject, varName, value, oldValue);

            if (varName == "len") {
                DreamList list = (DreamList)dreamObject;
                value.TryGetValueAsInteger(out var newLen);

                list.Resize(newLen);
            }
        }

        public DreamValue OnVariableGet(DreamObject dreamObject, string varName, DreamValue value)
        {
            switch (varName)
            {
                case "len":
                {
                    DreamList list = (DreamList)dreamObject;
                    return new DreamValue(list.GetLength());
                }
                case "type":
                    return new DreamValue(DreamPath.List);
                default:
                    return ParentType?.OnVariableGet(dreamObject, varName, value) ?? value;
            }
        }

        public ProcStatus OperatorAdd(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.AddValue(value);
                }
            } else {
                listCopy.AddValue(b);
            }

            state.Push(new DreamValue(listCopy));
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorSubtract(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.RemoveValue(value);
                }
            } else {
                listCopy.RemoveValue(b);
            }

            state.Push(new DreamValue(listCopy));
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorAppend(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    list.AddValue(value);
                }
            } else {
                list.AddValue(b);
            }
            state.Push(new DreamValue(list));
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorRemove(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                DreamValue[] values = bList.GetValues().ToArray();

                foreach (DreamValue value in values) {
                    list.RemoveValue(value);
                }
            } else {
                list.RemoveValue(b);
            }

            state.Push(a);
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorOr(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {    // List | List
                list = list.Union(bList);
            } else {                                                // List | x
                list = list.CreateCopy();
                list.AddValue(b);
            }

            state.Push(new DreamValue(list));
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorEquivalent(DreamValue a, DreamValue b, DMProcState state) {
            if (a.TryGetValueAsDreamList(out var firstList) && b.TryGetValueAsDreamList(out var secondList)) {
                if (firstList.GetLength() != secondList.GetLength())
                {
                    state.Push(new DreamValue(DreamValue.False));
                    return ProcStatus.Returned;
                }
                var firstValues = firstList.GetValues();
                var secondValues = secondList.GetValues();
                for (var i = 0; i < firstValues.Count; i++) {
                    if (!firstValues[i].Equals(secondValues[i]))
                    {
                        state.Push(new DreamValue(DreamValue.False));
                        return ProcStatus.Returned;
                    }
                }
                state.Push(new DreamValue(DreamValue.True));
                return ProcStatus.Returned;
            }
            state.Push(new DreamValue(DreamValue.False));
            return ProcStatus.Returned;// This will never be true, because reaching this line means b is not a list, while a will always be.
        }

        public ProcStatus OperatorCombine(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();

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
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorMask(DreamValue a, DreamValue b, DMProcState state) {
            DreamList list = a.GetValueAsDreamList();

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
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorIndex(DreamObject dreamObject, DreamValue index, DMProcState state) {
            state.Push(((DreamList)dreamObject).GetValue(index));
            return ProcStatus.Returned;
        }

        public ProcStatus OperatorIndexAssign(DreamObject dreamObject, DreamValue index, DreamValue value, DMProcState state) {
            ((DreamList)dreamObject).SetValue(index, value);
            state.Push(DreamValue.Null);
            return ProcStatus.Returned;
        }
    }
}
