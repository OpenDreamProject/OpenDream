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
                            if (insideBrackets) {
                                var bracketIndex = patternString.LastIndexOf('[', i);
                                var caret = patternString[bracketIndex + 1];
                                if(caret != '^')
                                    throw new NotImplementedException("OpenDream can't currently handle '\\L' inside of regex brackets unless it already contains '^'");
                            } else {
                                newPatternBuilder.Append('[');
                            }

                            newPatternBuilder.Append("A-Za-z\\n");
                            if (!insideBrackets)
                                newPatternBuilder.Append(']');
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
}
