using System.Text.RegularExpressions;
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

            if (!haystack.TryGetValueAsString(out var haystackString))
            {
                if (haystack.Value is null)
                {
                    return DreamValue.Null;
                }
                //TODO Check what actually happens
                throw new ArgumentException("Bad regex haystack");
            }
            string haystackSubstring = haystackString;
            if (end != 0) haystackSubstring = haystackString.Substring(0, end - start);

            if (replace.TryGetValueAsProc(out DreamProc replaceProc)) {
                return DoProcReplace(replaceProc);
            }
            if (replace.TryGetValueAsString(out string replaceString))
            {
                return DoTextReplace(replaceString);
            }

            if (replace.TryGetValueAsPath(out var procPath) && procPath.LastElement is not null)
            {
                var dreamMan = IoCManager.Resolve<IDreamManager>();
                if (dreamMan.GlobalProcs.ContainsKey(procPath.LastElement))
                {
                    var proc = dreamMan.GlobalProcs[procPath.LastElement];
                    return DoProcReplace(proc);

                }
            }

            throw new ArgumentException("Replacement argument must be a string or a proc");

            DreamValue DoProcReplace(DreamProc proc)
            {
                var match = dreamRegex.Regex.Match(haystackSubstring);
                var captures = match.Captures;
                List<DreamValue> args = new List<DreamValue>(captures.Count + 1);
                args.Add(new DreamValue(match.Value));
                foreach (var capture in captures)
                {
                    args.Add(capture is null ? DreamValue.Null : new DreamValue(capture));
                }
                var result = DreamThread.Run(async(state) => await state.Call(proc, instance, usr, new DreamProcArguments(args)));
                if (result.TryGetValueAsString(out var replacement))
                {
                    return DoTextReplace(replacement);
                }
                //TODO Confirm this behavior
                if (result.Value is null)
                {
                    return new DreamValue(haystackSubstring);
                }
                throw new ArgumentException("Replacement is not a string");
            }

            DreamValue DoTextReplace(string replacement)
            {
                string replaced = dreamRegex.Regex.Replace(haystackSubstring, replacement, dreamRegex.IsGlobal ? -1 : 1, start - 1);

                if(end != 0) replaced += haystackString.Substring(end - start + 1);

                instance.SetVariable("text", new DreamValue(replaced));
                return new DreamValue(replaced);
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
