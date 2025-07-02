using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace OpenDreamRuntime.ByondApi;

public enum ByondValueType : byte {
    // These are the actual value type values used by BYOND.
    // Even though these are an implementation detail, ByondApi users rely on these.

    // @formatter:off
    Null          = 0x00,
    Turf          = 0x01,
    Obj           = 0x02,
    Mob           = 0x03,
    Area          = 0x04,
    Client        = 0x05,
    String        = 0x06,
    MobTypePath   = 0x08,
    ObjTypePath   = 0x09,
    TurfTypePath  = 0x0A,
    AreaTypePath  = 0x0B,
    Image         = 0x0D,
    World         = 0x0E,
    List          = 0x0F,
    DatumTypePath = 0x20,
    Datum         = 0x21,
    Proc          = 0x26,
    Resource      = 0x27,
    Number        = 0x2A,
    Appearance    = 0x3A,
    Pointer       = 0x3C
    // @formatter:on
}

[StructLayout(LayoutKind.Explicit)]
public struct ByondValueData {
    [FieldOffset(0)]
    public uint @ref;

    [FieldOffset(0)]
    public float num;
}

public struct CByondValue {
    public ByondValueType type;
    public byte junk1, junk2, junk3;
    public ByondValueData data;
}

public struct CByondXYZ {
    public short x, y, z;
    public short junk;
}

public struct CByondPixLoc {
    public float x, y;
    public short z;
    public short junk;
};
