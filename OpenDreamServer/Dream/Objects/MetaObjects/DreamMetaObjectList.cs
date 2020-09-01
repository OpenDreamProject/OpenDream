using OpenDreamServer.Dream.Procs;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Objects.MetaObjects {
    class DreamMetaObjectList : DreamMetaObjectRoot {
        public static Dictionary<DreamObject, DreamList> DreamLists = new Dictionary<DreamObject, DreamList>();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamList list = new DreamList();

            DreamLists.Add(dreamObject, list);

            foreach (DreamValue orderedArgument in creationArguments.OrderedArguments) {
                list.AddValue(orderedArgument);
            }

            foreach (KeyValuePair<string, DreamValue> namedArgument in creationArguments.NamedArguments) {
                list.SetValue(new DreamValue(namedArgument.Key), namedArgument.Value);
            }

            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            DreamLists.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }

        public override void OnVariableSet(DreamObject dreamObject, string variableName, DreamValue variableValue, DreamValue oldVariableValue) {
            if (variableName == "len") {
                DreamList list = DreamLists[dreamObject];
                int newLen = variableValue.GetValueAsInteger();
                
                if (newLen > list.GetLength()) {
                    for (int i = list.GetLength(); i < newLen; i++) {
                        list.AddValue(new DreamValue((DreamObject)null));
                    }
                } else {
                    list.Cut(newLen + 1, list.GetLength() + 1);
                }
            } else {
                base.OnVariableSet(dreamObject, variableName, variableValue, oldVariableValue);
            }
        }

        public override DreamValue OnVariableGet(DreamObject dreamObject, string variableName, DreamValue variableValue) {
            if (variableName == "len") {
                DreamList list = DreamLists[dreamObject];

                return new DreamValue(list.GetLength());
            } else {
                return base.OnVariableGet(dreamObject, variableName, variableValue);
            }
        }

        public override DreamValue OperatorAdd(DreamValue a, DreamValue b) {
            DreamObject listObject = a.GetValueAsDreamObjectOfType(DreamPath.List);
            DreamObject listCopyObject = listObject.CallProc("Copy", new DreamProcArguments(null)).GetValueAsDreamObjectOfType(DreamPath.List);
            DreamList listCopy = DreamLists[listCopyObject];

            if (b.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject bValue = b.GetValueAsDreamObject();

                if (bValue != null && bValue.IsSubtypeOf(DreamPath.List)) {
                    DreamList bList = DreamLists[bValue];

                    foreach (DreamValue value in bList.GetValues()) {
                        listCopy.AddValue(value);
                    }
                } else {
                    listCopy.AddValue(b);
                }
            } else {
                listCopy.AddValue(b);
            }

            return new DreamValue(listCopyObject);
        }

        public override DreamValue OperatorSubtract(DreamValue a, DreamValue b) {
            DreamObject listObject = a.GetValueAsDreamObjectOfType(DreamPath.List);
            DreamObject listCopyObject = listObject.CallProc("Copy", new DreamProcArguments(null)).GetValueAsDreamObjectOfType(DreamPath.List);
            DreamList listCopy = DreamLists[listCopyObject];

            if (b.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject bValue = b.GetValueAsDreamObject();

                if (bValue != null && bValue.IsSubtypeOf(DreamPath.List)) {
                    DreamList bList = DreamLists[bValue];

                    foreach (DreamValue value in bList.GetValues()) {
                        if (listCopy.ContainsValue(value)) {
                            listCopy.RemoveValue(value);
                        }
                    }
                } else if (listCopy.ContainsValue(b)) {
                    listCopy.RemoveValue(b);
                }
            } else {
                if (listCopy.ContainsValue(b)) {
                    listCopy.RemoveValue(b);
                }
            }

            return new DreamValue(listCopyObject);
        }

        public override DreamValue OperatorAppend(DreamValue a, DreamValue b) {
            DreamObject listObject = a.GetValueAsDreamObjectOfType(DreamPath.List);
            DreamList list = DreamLists[listObject];

            if (b.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject bValue = b.GetValueAsDreamObject();

                if (bValue != null && bValue.IsSubtypeOf(DreamPath.List)) {
                    DreamList bList = DreamLists[bValue];

                    foreach (DreamValue value in bList.GetValues()) {
                        list.AddValue(value);
                    }
                } else {
                    list.AddValue(b);
                }
            } else {
                list.AddValue(b);
            }

            return a;
        }

        public override DreamValue OperatorRemove(DreamValue a, DreamValue b) {
            DreamObject listObject = a.GetValueAsDreamObjectOfType(DreamPath.List);
            DreamList list = DreamLists[listObject];

            if (b.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject bValue = b.GetValueAsDreamObject();

                if (bValue != null && bValue.IsSubtypeOf(DreamPath.List)) {
                    DreamList bList = DreamLists[bValue];

                    foreach (DreamValue value in bList.GetValues()) {
                        if (list.ContainsValue(value)) {
                            list.RemoveValue(value);
                        }
                    }
                } else if (list.ContainsValue(b)) {
                    list.RemoveValue(b);
                }
            } else {
                if (list.ContainsValue(b)) {
                    list.RemoveValue(b);
                }
            }

            return a;
        }

        public override DreamValue OperatorCombine(DreamValue a, DreamValue b) {
            DreamObject listObject = a.GetValueAsDreamObjectOfType(DreamPath.List);
            DreamList list = DreamLists[listObject];

            if (b.Type == DreamValue.DreamValueType.DreamObject) {
                DreamObject bValue = b.GetValueAsDreamObject();

                if (bValue != null && bValue.IsSubtypeOf(DreamPath.List)) {
                    DreamList bList = DreamLists[bValue];

                    foreach (DreamValue value in bList.GetValues()) {
                        if (!list.ContainsValue(value)) {
                            list.AddValue(value);
                        }
                    }
                } else if (!list.ContainsValue(b)) {
                    list.AddValue(b);
                }
            } else if (!list.ContainsValue(b)) {
                list.AddValue(b);
            }

            return a;
        }
    }
}
