﻿using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeList {
        [DreamProc("Add")]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Add(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamList list = (DreamList)src!;

            foreach (var argument in bundle.Arguments) {
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
        [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Copy(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            int start = bundle.GetArgument(0, "Start").MustGetValueAsInteger(); //1-indexed
            int end = bundle.GetArgument(1, "End").MustGetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)src!;
            DreamList listCopy = list.CreateCopy(start, end);

            return new DreamValue(listCopy);
        }

        [DreamProc("Cut")]
        [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Cut(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            int start = bundle.GetArgument(0, "Start").MustGetValueAsInteger(); //1-indexed
            int end = bundle.GetArgument(1, "End").MustGetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)src!;

            list.Cut(start, end);
            return DreamValue.Null;
        }

        [DreamProc("Find")]
        [DreamProcParameter("Elem")]
        [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Find(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamValue element = bundle.GetArgument(0, "Elem");
            if (!bundle.GetArgument(1, "Start").TryGetValueAsInteger(out var start)) //1-indexed
                start = 1; // 1 if non-number
            bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end); //1-indexed, 0 if non-number
            DreamList list = (DreamList)src!;

            return new(list.FindValue(element, start, end));
        }

        [DreamProc("Insert")]
        [DreamProcParameter("Index", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("Item1")]
        public static DreamValue NativeProc_Insert(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            int index = bundle.GetArgument(0, "Index").MustGetValueAsInteger(); //1-indexed
            DreamList list = (DreamList)src!;

            if (index <= 0) index = list.GetLength() + 1;
            if (bundle.Arguments.Length < 2) throw new Exception("No value given to insert");

            for (var i = 1; i < bundle.Arguments.Length; i++) {
                var item = bundle.Arguments[i];

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
        [DreamProcParameter("Glue", Type = DreamValueTypeFlag.String)]
        [DreamProcParameter("Start", Type = DreamValueTypeFlag.Float, DefaultValue = 1)]
        [DreamProcParameter("End", Type = DreamValueTypeFlag.Float, DefaultValue = 0)]
        public static DreamValue NativeProc_Join(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamList list = (DreamList)src!;
            List<DreamValue> values = list.GetValues();

            bundle.GetArgument(0, "Glue").TryGetValueAsString(out var glue);
            if (!bundle.GetArgument(1, "Start").TryGetValueAsInteger(out var start))
                start = 1;
            bundle.GetArgument(2, "End").TryGetValueAsInteger(out var end);

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
        public static DreamValue NativeProc_Remove(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamList list = (DreamList)src!;
            bool itemRemoved = false;

            foreach (var argument in bundle.Arguments) {
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
        [DreamProcParameter("Index1", Type = DreamValueTypeFlag.Float)]
        [DreamProcParameter("Index2", Type = DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Swap(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamList list = (DreamList)src!;
            int index1 = bundle.GetArgument(0, "Index1").MustGetValueAsInteger();
            int index2 = bundle.GetArgument(1, "Index2").MustGetValueAsInteger();

            list.Swap(index1, index2);
            return DreamValue.Null;
        }
    }
}
