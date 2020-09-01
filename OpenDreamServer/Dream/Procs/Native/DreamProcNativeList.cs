using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using OpenDreamShared.Dream;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OpenDreamServer.Dream.Procs.Native {
    static class DreamProcNativeList {
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

        public static DreamValue NativeProc_Copy(DreamProcScope scope, DreamProcArguments arguments) {
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];
            DreamList listCopy = list.CreateCopy(start, end);
            DreamObject newListObject = Program.DreamObjectTree.CreateObject(DreamPath.List, new DreamProcArguments(null));

            DreamMetaObjectList.DreamLists[newListObject] = listCopy;
            return new DreamValue(newListObject);
        }
        
        public static DreamValue NativeProc_Cut(DreamProcScope scope, DreamProcArguments arguments) {
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamList list = DreamMetaObjectList.DreamLists[scope.DreamObject];

            list.Cut(start, end);
            return new DreamValue((DreamObject)null);
        }

        public static DreamValue NativeProc_Find(DreamProcScope scope, DreamProcArguments arguments) {
            DreamValue element = scope.GetValue("Elem");
            int start = scope.GetValue("Start").GetValueAsInteger(); //1-indexed
            int end = scope.GetValue("End").GetValueAsInteger(); //1-indexed
            DreamObject listObject = scope.DreamObject;
            DreamList list = DreamMetaObjectList.DreamLists[listObject];

            if (start != 1 || end != 0) throw new NotImplementedException("Ranged /list.Find() is not implemented");
            return new DreamValue(list.FindValue(element));
        }

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
    }
}
