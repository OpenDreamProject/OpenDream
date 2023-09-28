using System;

namespace OpenDreamShared.Dream;

///<summary>
///Stores any explicit casting done via the "as" keyword. Also stores compiler hints for DMStandard.<br/>
///is a [Flags] enum because it's possible for something to have multiple values (especially with the quirky DMStandard ones)
/// </summary>
[Flags]
public enum DMValueType {
    Anything = 0x0,
    Null = 0x1,
    Text = 0x2,
    Obj = 0x4,
    Mob = 0x8,
    Turf = 0x10,
    Num = 0x20,
    Message = 0x40,
    Area = 0x80,
    Color = 0x100,
    File = 0x200,
    CommandText = 0x400,
    Sound = 0x800,
    Icon = 0x1000,
    //Byond here be dragons
    Unimplemented = 0x2000, // Marks that a method or property is not implemented. Throws a compiler warning if accessed.
    CompiletimeReadonly = 0x4000, // Marks that a property can only ever be read from, never written to. This is a const-ier version of const, for certain standard values like list.type
}
