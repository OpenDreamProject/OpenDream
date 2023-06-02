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

            // TODO Make this more Robust(TM)
            var anyLetterIdx = patternString.IndexOf("\\l", StringComparison.InvariantCulture); // From the ref: \l = Any letter A through Z, case-insensitive
            while (anyLetterIdx >= 0) {
                if (anyLetterIdx == 0 || patternString[anyLetterIdx - 1] != '\\') { // TODO Need to make this handle an arbitrary number of escape chars
                    patternString = patternString.Remove(anyLetterIdx, 2).Insert(anyLetterIdx, "[A-Za-z]");
                }

                var nextIdx = anyLetterIdx + 1;
                if(nextIdx >= patternString.Length) break;

                anyLetterIdx = patternString.IndexOf("\\l", nextIdx, StringComparison.InvariantCulture);
            }

            Regex = new Regex(patternString, options);
        } else {
            throw new Exception("Invalid regex pattern " + pattern);
        }
    }
}
