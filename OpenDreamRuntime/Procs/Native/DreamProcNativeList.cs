using System.Text;
using OpenDreamRuntime.Objects.Types;
using DreamValueType = OpenDreamRuntime.DreamValue.DreamValueType;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeList {
        [DreamProc("Add")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Add(NativeProc.State state) {
            DreamList list = (DreamList)state.Src!;

            foreach (var argument in state.Arguments.Values) {
                if (argument.TryGetValueAsDreamList(out var argumentList)) {
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
        public static DreamValue NativeProc_Copy(NativeProc.State state) {
            int start = state.GetArgument(0, "Start").GetValueAsInteger(); //1-indexed
            int end = state.GetArgument(1, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)state.Src!;
            DreamList listCopy = list.CreateCopy(start, end);

            return new DreamValue(listCopy);
        }

        [DreamProc("Cut")]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Cut(NativeProc.State state) {
            int start = state.GetArgument(0, "Start").GetValueAsInteger(); //1-indexed
            int end = state.GetArgument(1, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)state.Src!;

            list.Cut(start, end);
            return DreamValue.Null;
        }

        [DreamProc("Find")]
        [DreamProcParameter("Elem")]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Find(NativeProc.State state) {
            DreamValue element = state.GetArgument(0, "Elem");
            int start = state.GetArgument(1, "Start").GetValueAsInteger(); //1-indexed
            int end = state.GetArgument(2, "End").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)state.Src!;

            return new(list.FindValue(element, start, end));
        }

        [DreamProc("Insert")]
        [DreamProcParameter("Index", Type = DreamValueType.Float)]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Insert(NativeProc.State state) {
            int index = state.GetArgument(0, "Index").GetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)state.Src!;

            if (index <= 0) index = list.GetLength() + 1;
            if (state.Arguments.Count < 2) throw new Exception("No value given to insert");

            for (int i = 1; i < state.Arguments.Values.Length; i++) {
                var item = state.Arguments.Values[i];

                if (item.TryGetValueAsDreamList(out var valueList)) {
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
        [DreamProcParameter("Glue", Type = DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValueType.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueType.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Join(NativeProc.State state) {
            DreamList list = (DreamList)state.Src!;
            List<DreamValue> values = list.GetValues();

            state.GetArgument(0, "Glue").TryGetValueAsString(out var glue);
            if (!state.GetArgument(1, "Start").TryGetValueAsInteger(out var start))
                start = 1;
            state.GetArgument(2, "End").TryGetValueAsInteger(out var end);

            // Negative wrap-around
            if (end <= 0)
                end += values.Count + 1;
            if (start < 0)
                start += values.Count + 1;

            if (start == 0 || start >= end)
                return new(string.Empty);

            StringBuilder result = new(end - start);
            for (int i = start; i < end; i++) {
                result.Append(values[i - 1].Stringify());

                if (i != end - 1)
                    result.Append(glue);
            }

            return new DreamValue(result.ToString());
        }

        [DreamProc("Remove")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Remove(NativeProc.State state) {
            DreamList list = (DreamList)state.Src!;
            bool itemRemoved = false;

            foreach (var argument in state.Arguments.Values) {
                if (argument.TryGetValueAsDreamList(out var argumentList)) {
                    foreach (DreamValue value in argumentList.GetValues()) {
                        if (list.ContainsValue(value)) {
                            list.RemoveValue(value);

                            itemRemoved = true;
                        }
                    }
                } else {
                    if (list.ContainsValue(argument)) {
                        list.RemoveValue(argument);

                        itemRemoved = true;
                    }
                }
            }

            return new DreamValue(itemRemoved ? 1 : 0);
        }

        [DreamProc("Swap")]
        [DreamProcParameter("Index1", Type = DreamValueType.Float)]
        [DreamProcParameter("Index2", Type = DreamValueType.Float)]
        public static DreamValue NativeProc_Swap(NativeProc.State state) {
            DreamList list = (DreamList)state.Src!;
            int index1 = state.GetArgument(0, "Index1").GetValueAsInteger();
            int index2 = state.GetArgument(1, "Index2").GetValueAsInteger();

            list.Swap(index1, index2);
            return DreamValue.Null;
        }
    }
}
