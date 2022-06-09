using System.Text.RegularExpressions;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using DreamRegex = OpenDreamRuntime.Objects.MetaObjects.DreamMetaObjectRegex.DreamRegex;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeRegex {
        [DreamProc("Find")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValue.DreamValueType.Float | DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("End", DefaultValue = 0, Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Find(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamRegex dreamRegex = DreamMetaObjectRegex.ObjectToDreamRegex[instance];
            DreamValue haystack = arguments.GetArgument(0, "haystack");
            int next = GetNext(instance, arguments.GetArgument(1, "Start"), dreamRegex.IsGlobal);
            int end = arguments.GetArgument(2, "End").GetValueAsInteger();

            instance.SetVariable("text", haystack);

            string haystackString;
            if (!haystack.TryGetValueAsString(out haystackString)) {
                haystackString = String.Empty;
            }

            if (end == 0) end = haystackString.Length;
            if (haystackString.Length == next - 1) return new DreamValue(0);

            Match match = dreamRegex.Regex.Match(haystackString, next - 1, end - next);
            if (match.Success) {
                instance.SetVariable("index", new DreamValue(match.Index + 1));
                instance.SetVariable("match", new DreamValue(match.Value));
                if (match.Groups.Count > 0) {
                    DreamList groupList = DreamList.Create(match.Groups.Count);

                    for (int i = 1; i < match.Groups.Count; i++) {
                        groupList.AddValue(new DreamValue(match.Groups[i].Value));
                    }

                    instance.SetVariable("group", new DreamValue(groupList));
                }

                if (dreamRegex.IsGlobal) {
                    instance.SetVariable("next", new DreamValue(match.Index + match.Length));
                }

                return new DreamValue(match.Index + 1);
            } else {
                return new DreamValue(0);
            }
        }

        [DreamProc("Replace")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("replacement", Type = DreamValue.DreamValueType.String | DreamValue.DreamValueType.DreamProc)]
        [DreamProcParameter("Start", DefaultValue = 1, Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("End", DefaultValue = 0, Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Replace(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamRegex dreamRegex = DreamMetaObjectRegex.ObjectToDreamRegex[instance];

            DreamValue haystack = arguments.GetArgument(0, "haystack");
            DreamValue replace = arguments.GetArgument(1, "replacement");
            int start = arguments.GetArgument(2, "Start").GetValueAsInteger();
            int end = arguments.GetArgument(3, "End").GetValueAsInteger();

            string haystackString = haystack.GetValueAsString();
            string haystackSubstring = haystackString;
            if (end != 0) haystackSubstring = haystackString.Substring(0, end - start);

            if (replace.TryGetValueAsProc(out DreamProc replaceProc)) {
                throw new NotImplementedException("Proc regex replacements are not implemented");
            } else if (replace.TryGetValueAsString(out string replaceString))
            {
                string replaced = dreamRegex.Regex.Replace(haystackSubstring, replaceString, dreamRegex.IsGlobal ? -1 : 1, start - 1);

                if(end != 0) replaced += haystackString.Substring(end - start + 1);

                instance.SetVariable("text", new DreamValue(replaced));
                return new DreamValue(replaced);
            } else {
                throw new ArgumentException("Replacement argument must be a string");
            }
        }

        private static int GetNext(DreamObject regexInstance, DreamValue startParam, bool isGlobal) {
            if (startParam == DreamValue.Null) {
                if (isGlobal) {
                    DreamValue nextVar = regexInstance.GetVariable("next");

                    return (nextVar != DreamValue.Null) ? nextVar.GetValueAsInteger() : 1;
                } else {
                    return 1;
                }
            } else {
                return startParam.GetValueAsInteger();
            }
        }
    }
}
