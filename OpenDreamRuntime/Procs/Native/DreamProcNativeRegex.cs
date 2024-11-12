using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;

namespace OpenDreamRuntime.Procs.Native {
    internal static class DreamProcNativeRegex {
        [DreamProc("Find")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("start", Type = DreamValue.DreamValueTypeFlag.Float | DreamValue.DreamValueTypeFlag.DreamObject)] // BYOND docs say these are uppercase, they're not
        [DreamProcParameter("end", DefaultValue = 0, Type = DreamValue.DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Find(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamObjectRegex dreamRegex = (DreamObjectRegex)src!;
            DreamValue haystack = bundle.GetArgument(0, "haystack");

            if (!haystack.TryGetValueAsString(out var haystackString)) {
                haystackString = string.Empty;
            }

            int next = GetNext(src!, bundle.GetArgument(1, "start"), dreamRegex.IsGlobal, haystackString);
            int end = bundle.GetArgument(2, "end").GetValueAsInteger();

            dreamRegex.SetVariable("text", haystack);

            if (end == 0) end = haystackString.Length;
            if (haystackString.Length <= next - 1) {
                if (dreamRegex.IsGlobal) {
                    dreamRegex.SetVariable("next", DreamValue.Null);
                }
                return new DreamValue(0);
            }

            Match match = dreamRegex.Regex.Match(haystackString, Math.Clamp(next - 1, 0, haystackString.Length), end - next + 1);
            if (match.Success) {
                dreamRegex.SetVariable("index", new DreamValue(match.Index + 1));
                dreamRegex.SetVariable("match", new DreamValue(match.Value));
                if (match.Groups.Count > 0) {
                    DreamList groupList = bundle.ObjectTree.CreateList(match.Groups.Count);

                    for (int i = 1; i < match.Groups.Count; i++) {
                        groupList.AddValue(new DreamValue(match.Groups[i].Value));
                    }

                    dreamRegex.SetVariable("group", new DreamValue(groupList));
                }

                if (dreamRegex.IsGlobal) {
                    dreamRegex.SetVariable("next", new DreamValue(match.Index + match.Length + 1));
                }

                return new DreamValue(match.Index + 1);
            } else {
                if (dreamRegex.IsGlobal) {
                    dreamRegex.SetVariable("next", DreamValue.Null);
                }

                return new DreamValue(0);
            }
        }

        public static DreamValue RegexReplace(DreamObject regexInstance, DreamValue haystack, DreamValue replace, int start, int end) {
            DreamObjectRegex regex = (DreamObjectRegex)regexInstance;

            if (!haystack.TryGetValueAsString(out var haystackString)) {
                return DreamValue.Null;
            }

            string haystackSubstring = haystackString;
            if (end != 0) haystackSubstring = haystackString.Substring(0, end - start);

            if (replace.TryGetValueAsProc(out DreamProc? replaceProc)) {
                return DoProcReplace(replaceProc);
            }

            if (replace.TryGetValueAsString(out var replaceString)) {
                return DoTextReplace(replaceString);
            }

            throw new ArgumentException("Replacement argument must be a string or a proc");

            DreamValue DoProcReplace(DreamProc proc) {
                var currentStart = Math.Max(start - 1, 0);
                var currentHaystack = haystackSubstring;
                while (currentStart < currentHaystack.Length) {
                    var match = regex.Regex.Match(currentHaystack, currentStart);
                    if (!match.Success) break;

                    var groups = match.Groups;
                    var args = new DreamValue[groups.Count];
                    for (int i = 0; i < groups.Count; i++) {
                        args[i] = new DreamValue(groups[i].Value);
                    }

                    // TODO: src is the regex string
                    // TODO: We need to add this to our current thread instead of spawning a new one
                    // TODO: This call needs to immediately die upon sleeping
                    var result = proc.Spawn(null, new(args));

                    var replacement = result.Stringify();
                    currentHaystack = regex.Regex.Replace(currentHaystack, replacement, 1, currentStart);
                    currentStart = match.Index + Math.Max(replacement.Length, 1);

                    if (!regex.IsGlobal){
                        regex.SetVariable("next", new DreamValue(currentStart + 1));
                        break;
                    }
                }

                var replaced = currentHaystack;
                if (end != 0) replaced += haystackString.Substring(end - start + 1);

                regexInstance.SetVariable("text", new DreamValue(replaced));
                return new DreamValue(replaced);
            }

            DreamValue DoTextReplace(string replacement) {
                if(!regex.IsGlobal) {
                    var match = regex.Regex.Match(haystackString, Math.Clamp(start - 1, 0, haystackSubstring.Length));
                    if (!match.Success) return new DreamValue(haystackString);
                    regexInstance.SetVariable("next", new DreamValue(match.Index + Math.Max(replacement.Length, 1)));
                }

                string replaced = regex.Regex.Replace(haystackSubstring, replacement, regex.IsGlobal ? -1 : 1,
                    Math.Clamp(start - 1, 0, haystackSubstring.Length));

                if (end != 0) replaced += haystackString.Substring(end - start + 1);

                regexInstance.SetVariable("text", new DreamValue(replaced));
                return new DreamValue(replaced);
            }
        }

        [DreamProc("Replace")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueTypeFlag.String)]
        [DreamProcParameter("replacement", Type = DreamValue.DreamValueTypeFlag.String | DreamValue.DreamValueTypeFlag.DreamProc)]
        [DreamProcParameter("start", DefaultValue = 1, Type = DreamValue.DreamValueTypeFlag.Float)] // BYOND docs say these are uppercase, they're not
        [DreamProcParameter("end", DefaultValue = 0, Type = DreamValue.DreamValueTypeFlag.Float)]
        public static DreamValue NativeProc_Replace(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
            DreamValue haystack = bundle.GetArgument(0, "haystack");
            DreamValue replacement = bundle.GetArgument(1, "replacement");
            int start = bundle.GetArgument(2, "start").GetValueAsInteger();
            int end = bundle.GetArgument(3, "end").GetValueAsInteger();

            return RegexReplace(src, haystack, replacement, start, end);
        }

        private static int GetNext(DreamObject regexInstance, DreamValue startParam, bool isGlobal, string haystackString) {
            if (startParam.IsNull) {
                if (isGlobal && regexInstance.GetVariable("text").TryGetValueAsString(out string? lastHaystack) && lastHaystack == haystackString) {
                    DreamValue nextVar = regexInstance.GetVariable("next");

                    return (!nextVar.IsNull) ? nextVar.GetValueAsInteger() : 1;
                } else {
                    return 1;
                }
            } else {
                return startParam.GetValueAsInteger();
            }
        }
    }
}
