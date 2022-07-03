using OpenDreamRuntime.Objects;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeList {
        [DreamProc("Add")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Add(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = (DreamList)instance;

            foreach (DreamValue argument in arguments.OrderedArguments) {
                if (argument.TryGetValueAsDreamList(out DreamList argumentList)) {
                    foreach (DreamValue value in argumentList.GetValues()) {
                        list.AddValue(value);
                    }
                } else {
                    list.AddValue(argument);
                }
            }

            return DreamValue.Null;
        }

        [DreamProc("Copy")]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Copy(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int start = arguments.GetArgument(0, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(1, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)instance;
            DreamList listCopy = list.CreateCopy(start, end);

            return new DreamValue(listCopy);
        }

        [DreamProc("Cut")]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Cut(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int start = arguments.GetArgument(0, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(1, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)instance;

            list.Cut(start, end);
            return DreamValue.Null;
        }

        [DreamProc("Find")]
        [DreamProcParameter("Elem")]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Find(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamValue element = arguments.GetArgument(0, "Elem");
            int start = arguments.GetArgument(1, "Start").GetValueAsInteger(); //1-indexed
            int end = arguments.GetArgument(2, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)instance;

            return new(list.FindValue(element, start, end));
        }

        [DreamProc("Insert")]
        [DreamProcParameter("Index", Type = DreamValueType.Float)]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Insert(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            int index = arguments.GetArgument(0, "Index").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)instance;

            if (arguments.OrderedArguments.Count < 2) throw new Exception("No value given to insert");

            for (int i = 1; i < arguments.OrderedArguments.Count; i++) {
                DreamValue item = arguments.OrderedArguments[i];

                if (item.TryGetValueAsDreamList(out DreamList valueList)) {
                    foreach (DreamValue value in valueList.GetValues()) {
                        list.Insert(index++, value);
                    }
                } else {
                    list.Insert(index++, item);
                }
            }

            return new DreamValue(index);
        }

        [DreamProc("Remove")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Remove(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = (DreamList)instance;
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
        [DreamProcParameter("Index1", Type = DreamValueType.Float)]
        [DreamProcParameter("Index2", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_Swap(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamList list = (DreamList)instance;
            int index1 = arguments.GetArgument(0, "Index1").GetValueAsInteger();
            int index2 = arguments.GetArgument(1, "Index2").GetValueAsInteger();

            list.Swap(index1, index2);
            return DreamValue.Null;
        }
    }
}
