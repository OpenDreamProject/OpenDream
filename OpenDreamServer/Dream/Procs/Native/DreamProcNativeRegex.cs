using OpenDreamServer.Dream.Objects;
using OpenDreamServer.Dream.Objects.MetaObjects;
using System.Text.RegularExpressions;
using DreamRegex = OpenDreamServer.Dream.Objects.MetaObjects.DreamMetaObjectRegex.DreamRegex;

namespace OpenDreamServer.Dream.Procs.Native {
    static class DreamProcNativeRegex {
        [DreamProc("Find")]
        [DreamProcParameter("haystack", Type = DreamValue.DreamValueType.String)]
        [DreamProcParameter("Start", Type = DreamValue.DreamValueType.Integer)]
        [DreamProcParameter("End", DefaultValue = 0, Type = DreamValue.DreamValueType.Integer)]
        public static DreamValue NativeProc_Find(DreamObject instance, DreamObject usr, DreamProcArguments arguments) {
            DreamRegex dreamRegex = DreamMetaObjectRegex.ObjectToDreamRegex[instance];
            DreamValue haystack = arguments.GetArgument(0, "haystack");
            DreamValue start = arguments.GetArgument(1, "Start");
            int end = arguments.GetArgument(2, "End").GetValueAsInteger();

            int next;
            if (start == DreamValue.Null) {
                DreamValue nextVar = instance.GetVariable("next");

                next = (nextVar != DreamValue.Null) ? nextVar.GetValueAsInteger() : 1;
            } else {
                next = start.GetValueAsInteger();
            }

            instance.SetVariable("text", haystack);

            string haystackString = haystack.GetValueAsString();
            if (end == 0) end = haystackString.Length;
            if (haystackString.Length == next - 1) return new DreamValue(0);

            Match match = dreamRegex.Regex.Match(haystackString, next - 1, end - next);
            if (match.Success) {
                instance.SetVariable("index", new DreamValue(match.Index + 1));
                instance.SetVariable("match", new DreamValue(match.Value));
                if (match.Groups.Count > 0) {
                    DreamList groupList = Program.DreamObjectTree.CreateList();

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
    }
}
