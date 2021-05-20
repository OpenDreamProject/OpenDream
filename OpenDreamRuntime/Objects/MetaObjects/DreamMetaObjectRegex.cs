using OpenDreamVM.Procs;
using OpenDreamShared.Dream;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenDreamVM.Objects.MetaObjects {
    class DreamMetaObjectRegex : DreamMetaObjectDatum {
        public DreamMetaObjectRegex(DreamRuntime runtime)
            : base(runtime)
        {}

        public struct DreamRegex {
            public Regex Regex;
            public bool IsGlobal;
        }

        public static Dictionary<DreamObject, DreamRegex> ObjectToDreamRegex = new();

        public override void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamValue pattern = creationArguments.GetArgument(0, "pattern");
            DreamValue flags = creationArguments.GetArgument(1, "flags");
            DreamRegex regex;

            if (pattern.TryGetValueAsDreamObjectOfType(DreamPath.Regex, out DreamObject copyFrom)) {
                regex = ObjectToDreamRegex[dreamObject];
            } else if (pattern.TryGetValueAsString(out string patternString)) {
                regex = new DreamRegex();

                RegexOptions options = RegexOptions.None;
                if (flags.TryGetValueAsString(out string flagsString)) {
                    if (flagsString.Contains("i")) options |= RegexOptions.IgnoreCase;
                    if (flagsString.Contains("m")) options |= RegexOptions.Multiline;
                    if (flagsString.Contains("g")) regex.IsGlobal = true;
                }

                regex.Regex = new Regex(patternString, options);
            } else {
                throw new System.Exception("Invalid regex pattern " + pattern);
            }

            lock (ObjectToDreamRegex) {
                ObjectToDreamRegex.Add(dreamObject, regex);
            }

            base.OnObjectCreated(dreamObject, creationArguments);
        }
    }
}
