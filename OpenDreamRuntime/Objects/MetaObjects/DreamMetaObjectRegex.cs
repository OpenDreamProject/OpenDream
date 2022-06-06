using System.Text.RegularExpressions;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectRegex : DreamMetaObjectDatum {
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
                regex = ObjectToDreamRegex[copyFrom];
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

            ObjectToDreamRegex.Add(dreamObject, regex);
            base.OnObjectCreated(dreamObject, creationArguments);
        }

        public override void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamRegex.Remove(dreamObject);

            base.OnObjectDeleted(dreamObject);
        }
    }
}
