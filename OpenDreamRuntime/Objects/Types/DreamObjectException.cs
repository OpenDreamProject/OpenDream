namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectException(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    public DreamValue Name = DreamValue.Null;
    public string Description = string.Empty;
    public string File = string.Empty;
    public int Line = 0;

    //TODO: Match the format of BYOND exceptions since SS13 does splittext and other things to extract data from exceptions

    protected override void SetVar(string varName, DreamValue value) {
        switch (varName) { //set the internal vars of the C# object, then pass through to base.setvar to handle DM side
            case "name":
                Name = value;
                break;
            case "desc":
                if(value.TryGetValueAsString(out var stringDescription))
                    Description = stringDescription;
                break;
            case "file":
                if(value.TryGetValueAsString(out var stringFile))
                    File = stringFile;
                break;
            case "line":
                if(value.TryGetValueAsInteger(out var intLine))
                    Line = intLine;
                break;
        }
        base.SetVar(varName, value);
    }

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "name":
                value = Name;
                return true;
            case "desc":
                value = new DreamValue(Description);
                return true;
            case "file":
                value = new DreamValue(File);
                return true;
            case "line":
                value = new DreamValue(Line);
                return true;
            default:
                return base.TryGetVar(varName, out value);
        }
    }
}
