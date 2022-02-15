using System;
using System.ComponentModel;

namespace OpenDreamShared.Dream.Procs;

[Flags]
[DefaultValue(None)]
public enum ProcAttributes : byte
{
    None = 1 << 0, // Internal
    IsOverride = 1 << 1, // Internal
    Unimplemented = 1 << 2,
    Hidden = 1 << 3,
    HidePopupMenu = 1 << 4,
    DisableWaitfor = 1 << 5
}
