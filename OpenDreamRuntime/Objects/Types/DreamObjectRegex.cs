using System.Text;
using System.Text.RegularExpressions;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectRegex(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public override bool ShouldCallNew => false;

    public Regex Regex;
    public bool IsGlobal;

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        var pattern = args.GetArgument(0);
        var flags = args.GetArgument(1);

        if (pattern.TryGetValueAsDreamObject<DreamObjectRegex>(out var copyFrom)) {
            Regex = copyFrom.Regex;
            IsGlobal = copyFrom.IsGlobal;
        } else if (pattern.TryGetValueAsString(out var patternString)) {
            var options = RegexOptions.None;
            if (flags.TryGetValueAsString(out var flagsString)) {
                if (flagsString.Contains('i')) options |= RegexOptions.IgnoreCase;
                if (flagsString.Contains('m')) options |= RegexOptions.Multiline;
                if (flagsString.Contains('g')) IsGlobal = true;
            }

            // BYOND has some escape codes C# doesn't understand, like \l and \L
            // We need to replace those with ones it does understand
            StringBuilder newPatternBuilder = new(patternString.Length);
            bool insideBrackets = false;
            for (var i = 0; i < patternString.Length; i++) {
                char c = patternString[i];

                switch (c) {
                    case '\\':
                        c = patternString[++i];

                        if (c == 'l') {
                            if (!insideBrackets) // \l can be used inside [], so don't append more brackets if so
                                newPatternBuilder.Append('[');
                            newPatternBuilder.Append("A-Za-z");
                            if (!insideBrackets)
                                newPatternBuilder.Append(']');
                        } else if (c == 'L') {
                            if (!insideBrackets) {
                                newPatternBuilder.Append('[');
                            }

                            // TODO: This should really be "\W0-9_-[\n]" but "-[\n]" doesn't work unless it's at the end
                            newPatternBuilder.Append("\\W0-9_");

                            if (!insideBrackets)
                                newPatternBuilder.Append(']');
                        } else if (c == '_') {
                            newPatternBuilder.Append('_'); //I don't know why BYOND supports escaping this, but C# doesn't
                        } else {
                            newPatternBuilder.Append('\\');
                            goto default;
                        }

                        continue;
                    case '[':
                        insideBrackets = true;
                        goto default;
                    case ']' when insideBrackets:
                        insideBrackets = false;
                        goto default;
                    default:
                        newPatternBuilder.Append(c);
                        continue;
                }
            }

            Regex = new Regex(newPatternBuilder.ToString(), options);
        } else {
            throw new Exception("Invalid regex pattern " + pattern);
        }
    }

    public DreamValue FindHelper(string haystackString, int start, int end) {
        Match match = Regex.Match(haystackString, start, end);
        if (match.Success) {
            SetVariable("index", new DreamValue(match.Index + 1));
            SetVariable("match", new DreamValue(match.Value));
            if (match.Groups.Count > 0) {
                DreamList groupList = ObjectTree.CreateList(match.Groups.Count);

                for (int i = 1; i < match.Groups.Count; i++) {
                    groupList.AddValue(new DreamValue(match.Groups[i].Value));
                }

                SetVariable("group", new DreamValue(groupList));
            }

            if (IsGlobal) {
                SetVariable("next", new DreamValue(match.Index + match.Length + 1));
            }

            return new DreamValue(match.Index + 1);
        }

        if (IsGlobal) {
            SetVariable("next", DreamValue.Null);
        }

        return new DreamValue(0);
    }
}
