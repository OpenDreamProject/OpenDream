using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using DreamValueType = OpenDreamServer.Dream.DreamValue.DreamValueType;

namespace OpenDreamServer.Dream.Procs.Native {
    static class DreamProcNativeList {
        [DreamProc("Add")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Add(DreamProcScope scope, DreamProcArguments arguments) {
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            foreach (DreamValue argument in arguments.OrderedArguments) {
                if (argument.TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject argumentListObject)) {
                    DreamList argumentList = DreamMetaObjectList.DreamLists[argumentListObject];

                    foreach (DreamValue value in argumentList.GetValues()) {
                        list.AddValue(value);
                    }
                } else {
                    list.AddValue(argument);
                }
            }

            return new DreamValue((DreamObject)null);
        }

        [DreamProc("Copy")]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_Copy(DreamProcScope scope, DreamProcArguments arguments) {
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];
            DreamList listCopy = list.CreateCopy(start, end);
            DreamObject newListObject = Program.DreamObjectTree.CreateObject(DreamPath.List);

            DreamMetaObjectList.DreamLists[newListObject] = listCopy;
            return new DreamValue(newListObject);
        }

        [DreamProc("Cut")]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_Cut(DreamProcScope scope, DreamProcArguments arguments) {
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamList list = DreamMetaObjectList.DreamLists[scope.DreamObject];

            list.Cut(start, end);
            return new DreamValue((DreamObject)null);
        }

        [DreamProc("Find")]
        [DreamProcParameter("Elem")]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_Find(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue element = scope.GetValue("Elem");
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            if (start != 1 || end != 0) throw new NotImplementedException("Ranged /list.Find() is not implemented");
            return new DreamValue(list.FindValue(element));
        }

        [DreamProc("Insert")]
        [DreamProcParameter("Index", Type = DreamValueType.Integer)]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Insert(DreamProcScope scope, DreamProcArguments arguments) {
            int index = scope.GetValue("Index").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            if (arguments.OrderedArguments.Count < 2) throw new Exception("No value given to insert");

            for (int i = 1; i < arguments.OrderedArguments.Count; i++) {
                DreamValue item = arguments.OrderedArguments[i];

                if (item.TryGetValueAsDreamObjectOfType(DreamPath.List, out DreamObject valueListObject)) {
                    DreamList valueList = DreamMetaObjectList.DreamLists[valueListObject];

                    foreach (DreamValue value in valueList.GetValues()) {
                        list.Insert(index++, value);
                    }
                } else {
                    list.Insert(index++, item);
                }
            }

            return new DreamValue(index);
        }

        [DreamProc("Join")]
        [DreamProcParameter("Glue")]
        [DreamProcParameter("Start", Type = DreamValueType.Integer, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Integer, DefaultValue = 0)]
        public static DreamValue NativeProc_Join(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue glue = scope.GetValue("Glue");
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            string glueValue = (glue.Type == DreamValueType.String) ? glue.GetValueAsString() : "";
            return new DreamValue(list.Join(glueValue, start, end));
        }

        [DreamProc("Remove")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Remove(DreamProcScope scope, DreamProcArguments arguments) {
            DreamList list = DreamMetaObjectList.DreamLists[scope.DreamObject];
            List<DreamValue> argumentValues = arguments.GetAllArguments();
            bool itemRemoved = false;

            foreach (DreamValue argument in argumentValues) {
                if (list.ContainsValue(argument)) {
                    list.RemoveValue(argument);

                    itemRemoved = true;
                }
            }

            return new DreamValue(itemRemoved ? 1 : 0);
        }

        [DreamProc("Swap")]
        [DreamProcParameter("Index1", Type = DreamValueType.Integer)]
        [DreamProcParameter("Index2", Type = DreamValueType.Integer)]
        public static DreamValue NativeProc_Swap(DreamProcScope scope, DreamProcArguments arguments) {
            DreamList list = DreamMetaObjectList.DreamLists[scope.DreamObject];
            int index1 = scope.GetValue("Index1").GetValueAsInteger();
            int index2 = scope.GetValue("Index2").GetValueAsInteger();

            list.Swap(index1, index2);
            return new DreamValue((DreamObject)null);
        }
    }
}
