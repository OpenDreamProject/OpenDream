using System.Runtime.CompilerServices;

namespace OpenDreamRuntime;

public static unsafe partial class ByondApi {
    // Trust me bro, I know ""u8 has a null terminator...
    public static byte* PinningIsNotReal(ReadOnlySpan<byte> nullTerminated)
    {
        return (byte*)Unsafe.AsPointer(ref Unsafe.AsRef(in nullTerminated[0]));
    }
}
