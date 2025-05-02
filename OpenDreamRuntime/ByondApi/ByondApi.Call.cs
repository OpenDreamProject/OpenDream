using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenDreamRuntime.ByondApi;

public static partial class ByondApi {
    public static string? CrashMessage;

    public static unsafe CByondValue DoCall(delegate* unmanaged[Cdecl]<uint, CByondValue*, CByondValue> func, ReadOnlySpan<CByondValue> args) {
        CrashMessage = null;

        CByondValue result;
        fixed (CByondValue* argV = args) {
            result = OpenDream_Internal_CallExt(func, (uint)args.Length, argV);
        }

        if (CrashMessage != null) {
            throw new ByondApiRuntime(CrashMessage);
        }

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void OpenDream_Internal_SetCrash(byte* message) {
        CrashMessage = Marshal.PtrToStringUTF8((nint) message);
    }

    [LibraryImport("byond")]
    private static unsafe partial CByondValue OpenDream_Internal_CallExt(
        delegate* unmanaged[Cdecl]<uint, CByondValue*, CByondValue> func,
        uint argc,
        CByondValue* argv);
}

/// <summary>
/// Represents a runtime error raised by a byondapi library.
/// </summary>
public sealed class ByondApiRuntime(string message) : Exception(message);
