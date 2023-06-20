using System;

namespace OpenDreamShared.Rendering;

// Same values as the SEE_* defines in DMStandard
[Flags]
public enum SightFlags {
    SeeInfrared = (1<<6),
    SeeSelf = (1<<5),
    SeeMobs = (1<<2),
    SeeObjs = (1<<3),
    SeeTurfs = (1<<4),
    SeePixels = (1<<8),
    SeeThroughOpaque = (1<<9),
    SeeBlackness = (1<<10),
    Blind = (1<<0)
}
