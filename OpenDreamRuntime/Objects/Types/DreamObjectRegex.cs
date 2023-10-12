using System.Text.RegularExpressions;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectRegex : DreamObject {
    public override bool ShouldCallNew => false;

    public Regex Regex;
    public bool IsGlobal;

    public DreamObjectRegex(DreamObjectDefinition objectDefinition) : base(objectDefinition) {

    }

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

            for(var i = 0; i < patternString.Length; i++) {
                if (patternString[i] != '\\')
                    continue;

                ++i; // Move to the first char of the escape code
                if (i >= patternString.Length)
                    throw new Exception($"Invalid escape code at end of regex {pattern}");

                // BYOND recognizes some escape codes C# doesn't. We replace those with their equivalent here.
                switch (patternString[i]) {
                    case 'l': // Any letter A through Z, case-insensitive
                        patternString = patternString.Remove(i - 1, 2).Insert(i - 1, "[A-Za-z]");
                        i += 6;
                        break;
                    case 'L': // Any character except a letter or line break
                        patternString = patternString.Remove(i - 1, 2).Insert(i - 1, "[^A-Za-z\\n]");
                        i += 9;
                        break;
                }
            }

            Regex = new Regex(patternString, options);
        } else {
            throw new Exception("Invalid regex pattern " + pattern);
        }
    }
}
