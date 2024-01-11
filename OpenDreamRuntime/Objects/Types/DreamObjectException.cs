namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectException : DreamObject {
    public string Name = string.Empty;
    public string Description = string.Empty;
    public string File = string.Empty;
    public string Line = string.Empty;

    //TODO: Match the format of BYOND exceptions since SS13 does splittext and other things to extract data from exceptions

    protected override bool TryGetVar(string varName, out DreamValue value) {
        switch (varName) {
            case "name":
                value = new DreamValue(Name);
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
