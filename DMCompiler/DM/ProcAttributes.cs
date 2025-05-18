namespace DMCompiler.DM;

[Flags]
public enum ProcAttributes {
    None = 1 << 0, // Internal
    IsOverride = 1 << 1, // Internal
    Unimplemented = 1 << 2,
    Unsupported = 1 << 3,
    Hidden = 1 << 4,
    HidePopupMenu = 1 << 5,
    DisableWaitfor = 1 << 6,
    Instant = 1 << 7,
    Background = 1 << 8
}
