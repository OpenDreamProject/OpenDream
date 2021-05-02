using OpenDreamServer.Dream.Procs;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectList : DreamMetaObjectRoot {
        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            base.OnObjectCreated(dreamObject, creationArguments);

            dreamObject.CallProc("New", creationArguments);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);

            if (variableName == "len") {
                DreamList list = (DreamList)dreamObject;
                int newLen = variableValue.GetValueAsInteger();

                list.Resize(newLen);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "len") {
                DreamList list = (DreamList)dreamObject;

                return new DreamValue(list.GetLength());
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            DreamList list = a.GetValueAsDreamList();
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

        public override DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            DreamList list = a.GetValueAsDreamList();
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

        public override DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            DreamList list = a.GetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                foreach (DreamValue value in bList.GetValues()) {
                    list.AddValue(value);
                }
            } else {
                list.AddValue(b);
            }

            return a;
        }

        public override DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            DreamList list = a.GetValueAsDreamList();

            if (b.TryGetValueAsDreamList(out DreamList bList)) {
                DreamValue[] values = bList.GetValues().ToArray();

                foreach (DreamValue value in values) {
                    list.RemoveValue(value);
                }
            } else {
                list.RemoveValue(b);
            }

            return a;
        }

        public override DreamValue OperatorCombine(DreamValue a, DreamValue b) {
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

            return a;
        }

        public override DreamValue OperatorMask(DreamValue a, DreamValue b) {
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

            return a;
        }
    }
}
