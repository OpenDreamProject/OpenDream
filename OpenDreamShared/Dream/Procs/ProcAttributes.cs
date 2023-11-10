using System;

namespace OpenDreamShared.Dream.Procs;

[Flags]
public enum ProcAttributes
{
    None = 1 << 0, // Internal
    IsOverride = 1 << 1, // Internal
    Unimplemented = 1 << 2,
    Hidden = 1 << 3,
    HidePopupMenu = 1 << 4,
    DisableWaitfor = 1 << 5,
    Instant = 1 << 6,
    Background = 1 << 7
}
