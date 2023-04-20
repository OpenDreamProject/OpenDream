using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.MetaObjects;
using OpenDreamShared.Dream;
using DreamRegex = OpenDreamRuntime.Objects.MetaObjects.DreamMetaObjectRegex.DreamRegex;

namespace OpenDreamRuntime.Procs.Native {
    static class DreamProcNativeRegex {
        [DreamProc("Find")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValue.DreamValueType.Float | DreamValue.DreamValueType.DreamObject)]
        [DreamProcParameter("End", DefaultValue = 0, Type = DreamValue.DreamValueType.Float)]
        public static DreamValue NativeProc_Find(NativeProc.State state) {
            DreamRegex dreamRegex = DreamMetaObjectRegex.ObjectToDreamRegex[state.Src];
            DreamValue haystack = state.Arguments.GetArgument(0, "haystack");
            int next = GetNext(state.Src, state.Arguments.GetArgument(1, "Start"), dreamRegex.IsGlobal);
            int end = state.Arguments.GetArgument(2, "End").GetValueAsInteger();

            state.Src.SetVariable("text", haystack);

            string haystackString;
            if (!haystack.TryGetValueAsString(out haystackString)) {
                haystackString = String.Empty;
            }

            if (end == 0) end = haystackString.Length;
            if (haystackString.Length == next - 1) return new DreamValue(0);

            Match match = dreamRegex.Regex.Match(haystackString, next - 1, end - next);
            if (match.Success) {
                state.Src.SetVariable("index", new DreamValue(match.Index + 1));
                state.Src.SetVariable("match", new DreamValue(match.Value));
                if (match.Groups.Count > 0) {
                    DreamList groupList = state.ObjectTree.CreateList(match.Groups.Count);

                    for (int i = 1; i < match.Groups.Count; i++) {
                        groupList.AddValue(new DreamValue(match.Groups[i].Value));
                    }

                    state.Src.SetVariable("group", new DreamValue(groupList));
                }

                if (dreamRegex.IsGlobal) {
                    state.Src.SetVariable("next", new DreamValue(match.Index + match.Length + 1));
                }

                return new DreamValue(match.Index + 1);
            } else {
                return new DreamValue(0);
            }
        }

        public static async Task<DreamValue> RegexReplace(AsyncNativeProc.State state, DreamObject regexInstance, DreamValue haystack, DreamValue replace,
            int start, int end) {
            DreamRegex regex = DreamMetaObjectRegex.ObjectToDreamRegex[regexInstance];

            if (!haystack.TryGetValueAsString(out var haystackString)) {
                if (haystack == DreamValue.Null) {
                    return DreamValue.Null;
                }

                //TODO Check what actually happens
                throw new ArgumentException("Bad regex haystack");
            }

            string haystackSubstring = haystackString;
            if (end != 0) haystackSubstring = haystackString.Substring(0, end - start);

            if (replace.TryGetValueAsProc(out DreamProc replaceProc)) {
                return await DoProcReplace(state, replaceProc);
            }

            if (replace.TryGetValueAsString(out var replaceString)) {
                return DoTextReplace(replaceString);
            }

            throw new ArgumentException("Replacement argument must be a string or a proc");

            async Task<DreamValue> DoProcReplace(AsyncNativeProc.State state, DreamProc proc) {
                if (regex.IsGlobal) {
                    throw new NotImplementedException("Proc global regex replacements are not implemented");
                }

                var match = regex.Regex.Match(haystackSubstring);
                var groups = match.Groups;
                List<DreamValue> args = new List<DreamValue>(groups.Count);
                foreach (Group group in groups) {
                    args.Add(new DreamValue(group.Value));
                }

                var result = await state.CallNoWait(proc, regexInstance, null, new DreamProcArguments(args));

                if (result.TryGetValueAsString(out var replacement)) {
                    return DoTextReplace(replacement);
                }

                //TODO Confirm this behavior
                if (result == DreamValue.Null) {
                    return new DreamValue(haystackSubstring);
                }

                throw new ArgumentException("Replacement is not a string");
            }

            DreamValue DoTextReplace(string replacement) {
                string replaced = regex.Regex.Replace(haystackSubstring, replacement, regex.IsGlobal ? -1 : 1,
                    start - 1);

                if (end != 0) replaced += haystackString.Substring(end - start + 1);

                regexInstance.SetVariable("text", new DreamValue(replaced));
                return new DreamValue(replaced);
            }
        }

        [DreamProc("Replace")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("replacement", Type = DreamValue.DreamValueType.String | DreamValue.DreamValueType.DreamProc)]
        [DreamProcParameter("Start", DefaultValue = 1, Type = DreamValue.DreamValueType.Float)]
        [DreamProcParameter("End", DefaultValue = 0, Type = DreamValue.DreamValueType.Float)]
        public static async Task<DreamValue> NativeProc_Replace(AsyncNativeProc.State state) {
            DreamValue haystack = state.Arguments.GetArgument(0, "haystack");
            DreamValue replacement = state.Arguments.GetArgument(1, "replacement");
            int start = state.Arguments.GetArgument(2, "Start").GetValueAsInteger();
            int end = state.Arguments.GetArgument(3, "End").GetValueAsInteger();

            return await RegexReplace(state, state.Src, haystack, replacement, start, end);
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
