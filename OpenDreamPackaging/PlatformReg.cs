namespace OpenDreamPackaging;

public record PlatformReg(string RId, string TargetOs, bool BuildByDefault) {
    public string RId = RId;
    public string TargetOs = TargetOs;
    public bool BuildByDefault = BuildByDefault;
}
