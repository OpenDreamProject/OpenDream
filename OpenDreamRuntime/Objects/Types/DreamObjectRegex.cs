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

            var parsingString = "";
            for(var i = 0; i < patternString.Length; i++) {
                parsingString += patternString[i];
                if(parsingString == "\\\\") {
                    parsingString = "";
                    continue;
                }

                if(parsingString == "\\l") {
                    patternString = patternString.Remove(i-1, 2).Insert(i-1, "[A-Za-z]");
                    parsingString = ""; 
                    i += 6;
                    continue;
                }

                if(parsingString == "\\L") {
                    patternString = patternString.Remove(i - 1, 2).Insert(i - 1, "[^A-Za-z\\n]");
                    parsingString = "";
                    i += 9;
                }
            }
            Regex = new Regex(patternString, options);
        } else {
            throw new Exception("Invalid regex pattern " + pattern);
        }
    }
}
