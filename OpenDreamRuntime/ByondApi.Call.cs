using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenDreamRuntime;

public static partial class ByondApi {
    public static string? CrashMessage;

    public unsafe static CByondValue DoCall(
        delegate* unmanaged[Cdecl]<uint, ByondApi.CByondValue*, ByondApi.CByondValue> func,
        ReadOnlySpan<CByondValue> args) {

        CrashMessage = null;

        ByondApi.CByondValue result;
        fixed (ByondApi.CByondValue* argV = args) {
            result = OpenDream_Internal_CallExt(func, (uint)args.Length, argV);
        }

        if (CrashMessage != null) {
            throw new ByondApiRuntime(CrashMessage);
        }

        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private unsafe static void OpenDream_Internal_SetCrash(byte* message) {
        CrashMessage = Marshal.PtrToStringUTF8((nint) message);
    }

    [LibraryImport("byond")]
    private static unsafe partial CByondValue OpenDream_Internal_CallExt(
        delegate* unmanaged[Cdecl]<uint, ByondApi.CByondValue*, ByondApi.CByondValue> func,
        uint argc,
        CByondValue* argv);
}

/// <summary>
/// Represents a runtime error raised by a byondapi library.
/// </summary>
public sealed class ByondApiRuntime(string message) : Exception(message);
