using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using Robust.Server.Player;
using Robust.Shared.Timing;

namespace OpenDreamRuntime {
    // TODO: Could probably use DreamValueType instead
    public enum RefType : uint {
        Null = 0x0,
        DreamObjectTurf = 0x1000000,
        DreamObject = 0x2000000,
        DreamObjectMob = 0x3000000,
        DreamObjectArea = 0x4000000,
        DreamObjectClient = 0x5000000,
        DreamObjectImage = 0xD000000,
        DreamObjectList = 0xF000000,
        DreamObjectDatum = 0x21000000,
        String = 0x6000000,
        DreamType = 0x9000000, //in byond type is from 0x8 to 0xb, but fuck that
        DreamResource = 0x27000000, //Equivalent to file
        DreamAppearance = 0x3A000000,
        Proc = 0x26000000
    }
}
