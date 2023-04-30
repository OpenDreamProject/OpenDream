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
                                    DreamList newList = _objectTree.CreateList();

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

        public DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            DreamList list = a.MustGetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.AddValue(value);
                }
            } else {
                listCopy.AddValue(b);
            }

            return new DreamValue(listCopy);
        }

        public DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            DreamList list = a.MustGetValueAsDreamList();
            DreamList listCopy = list.CreateCopy();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    listCopy.RemoveValue(value);
                }
            } else {
                listCopy.RemoveValue(b);
            }

            return new DreamValue(listCopy);
        }

        public DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    list.AddValue(value);
                }
            } else {
                list.AddValue(b);
            }

            return a;
        }

        public DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out var bList)) {
                DreamValue[] values = bList.GetValues().ToArray();

                foreach (DreamValue value in values) {
                    list.RemoveValue(value);
                }
            } else {
                list.RemoveValue(b);
            }

            return a;
        }

        public DreamValue OperatorOr(DreamValue a, DreamValue b) {
            DreamList list = a.MustGetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {    // List | List
                list = list.Union(bList);
            } else {                                                // List | x
                list = list.CreateCopy();
                list.AddValue(b);
            }

            return new DreamValue(list);
        }

        public DreamValue OperatorEquivalent(DreamValue a, DreamValue b) {
            if (a.TryGetValueAsDreamList(out var firstList) && b.TryGetValueAsDreamList(out var secondList)) {
                if (firstList.GetLength() != secondList.GetLength()) return DreamValue.False;
                var firstValues = firstList.GetValues();
                var secondValues = secondList.GetValues();
                for (var i = 0; i < firstValues.Count; i++) {
                    if (!firstValues[i].Equals(secondValues[i])) return DreamValue.False;
                }

                return DreamValue.True;
            }
            return DreamValue.False; // This will never be true, because reaching this line means b is not a list, while a will always be.
        }

        public DreamValue OperatorNotEquivalent(DreamValue a, DreamValue b) {
            if(OperatorEquivalent(a,b) == DreamValue.True)
                return DreamValue.False;
            else
                return DreamValue.True;
        }
        public DreamValue OperatorCombine(DreamValue a, DreamValue b) {
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

            return a;
        }

        public DreamValue OperatorMask(DreamValue a, DreamValue b) {
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

            return a;
        }

        public DreamValue OperatorIndex(DreamObject dreamObject, DreamValue index) {
            return ((DreamList)dreamObject).GetValue(index);
        }

        public void OperatorIndexAssign(DreamObject dreamObject, DreamValue index, DreamValue value) {
            ((DreamList)dreamObject).SetValue(index, value);
        }
    }
}
