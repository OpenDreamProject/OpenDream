using System.Text.RegularExpressions;
using OpenDreamRuntime.Procs;
using OpenDreamShared.Dream;

namespace OpenDreamRuntime.Objects.MetaObjects {
    sealed class DreamMetaObjectRegex : IDreamMetaObject {
        public static readonly Dictionary<DreamObject, DreamRegex> ObjectToDreamRegex = new();

        public bool ShouldCallNew => false;
        public IDreamMetaObject? ParentType { get; set; }

        [Dependency] private readonly IDreamObjectTree _objectTree = default!;

        public DreamMetaObjectRegex() {
            IoCManager.InjectDependencies(this);
        }

        public struct DreamRegex {
            public readonly Regex Regex;
            public readonly bool IsGlobal;
            
            public DreamRegex(Regex regex, bool isGlobal) {
                Regex = regex;
                IsGlobal = isGlobal;
            }
        }

        public void OnObjectCreated(DreamObject dreamObject, DreamProcArguments creationArguments) {
            DreamValue pattern = creationArguments.GetArgument(0, "pattern");
            DreamValue flags = creationArguments.GetArgument(1, "flags");
            DreamRegex regex;

            if (pattern.TryGetValueAsDreamObjectOfType(_objectTree.Regex, out DreamObject copyFrom)) {
                regex = ObjectToDreamRegex[copyFrom];
            } else if (pattern.TryGetValueAsString(out string patternString)) {
                bool isGlobal = false;
                RegexOptions options = RegexOptions.None;
                if (flags.TryGetValueAsString(out string flagsString)) {
                    if (flagsString.Contains("i")) options |= RegexOptions.IgnoreCase;
                    if (flagsString.Contains("m")) options |= RegexOptions.Multiline;
                    if (flagsString.Contains("g")) isGlobal = true;
                }

                // TODO Make this more Robust(TM)
                var anyLetterIdx = patternString.IndexOf("\\l"); // From the ref: \l = Any letter A through Z, case-insensitive
                while (anyLetterIdx >= 0) {
                    if (anyLetterIdx == 0 || patternString[anyLetterIdx - 1] != '\\') { // TODO Need to make this handle an arbitrary number of escape chars
                            patternString = patternString.Remove(anyLetterIdx, 2).Insert(anyLetterIdx, "[A-Za-z]");
                    }

                    var nextIdx = anyLetterIdx + 1;
                    if(nextIdx >= patternString.Length) break;

                    anyLetterIdx = patternString.IndexOf("\\l", nextIdx);
                }

                regex = new DreamRegex(new Regex(patternString, options), isGlobal);
            } else {
                throw new System.Exception("Invalid regex pattern " + pattern);
            }

            ObjectToDreamRegex.Add(dreamObject, regex);
            ParentType?.OnObjectCreated(dreamObject, creationArguments);
        }

        public void OnObjectDeleted(DreamObject dreamObject) {
            ObjectToDreamRegex.Remove(dreamObject);

            ParentType?.OnObjectDeleted(dreamObject);
        }
    }
}
