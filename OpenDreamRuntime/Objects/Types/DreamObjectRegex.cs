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

        DreamValue pattern = args.GetArgument(0);
        DreamValue flags = args.GetArgument(1);

        if (pattern.TryGetValueAsDreamObject<DreamObjectRegex>(out var copyFrom)) {
            Regex = copyFrom.Regex;
            IsGlobal = copyFrom.IsGlobal;
        } else if (pattern.TryGetValueAsString(out var patternString)) {
            RegexOptions options = RegexOptions.None;
            if (flags.TryGetValueAsString(out var flagsString)) {
                if (flagsString.Contains("i")) options |= RegexOptions.IgnoreCase;
                if (flagsString.Contains("m")) options |= RegexOptions.Multiline;
                if (flagsString.Contains("g")) IsGlobal = true;
            }

            ReplaceEscapeCodes(ref patternString);
            Regex = new Regex(patternString, options);
        } else {
            throw new Exception("Invalid regex pattern " + pattern);
        }
    }

    /// <summary>
    /// Replaces escape codes that BYOND recognizes but C# doesn't
    /// </summary>
    private static void ReplaceEscapeCodes(ref string regex) {
        // TODO Make this more Robust(TM)

        var anyLetterIdx = regex.IndexOf("\\l", StringComparison.InvariantCulture); // From the ref: \l = Any letter A through Z, case-insensitive
        while (anyLetterIdx >= 0) {
            if (anyLetterIdx == 0 || regex[anyLetterIdx - 1] != '\\') { // TODO Need to make this handle an arbitrary number of escape chars
                regex = regex.Remove(anyLetterIdx, 2).Insert(anyLetterIdx, "[A-Za-z]");
            }

            var nextIdx = anyLetterIdx + 1;
            if(nextIdx >= regex.Length) break;

            anyLetterIdx = regex.IndexOf("\\l", nextIdx, StringComparison.InvariantCulture);
        }

        var anyButLetterIdx = regex.IndexOf("\\L", StringComparison.InvariantCulture); // From the ref: \L = Any character except a letter or line break
        while (anyButLetterIdx >= 0) {
            if (anyButLetterIdx == 0 || regex[anyButLetterIdx - 1] != '\\') { // TODO Need to make this handle an arbitrary number of escape chars
                regex = regex.Remove(anyButLetterIdx, 2).Insert(anyButLetterIdx, "[^A-Za-z\\n]");
            }

            var nextIdx = anyButLetterIdx + 1;
            if(nextIdx >= regex.Length) break;

            anyButLetterIdx = regex.IndexOf("\\L", nextIdx, StringComparison.InvariantCulture);
        }
    }
}
