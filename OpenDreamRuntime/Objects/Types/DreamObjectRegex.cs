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
                if (flagsString.Contains('i')) options |= RegexOptions.IgnoreCase;
                if (flagsString.Contains('m')) options |= RegexOptions.Multiline;
                if (flagsString.Contains('g')) IsGlobal = true;
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

        // Probably make this slightly saner
        // Just add chain calls as needed and pray performance doesnt explode
        regex = regex
            .Replace("\\l", "[A-Za-z]")
            .Replace("\\L", "[^A-Za-z\\n]");
    }
}
