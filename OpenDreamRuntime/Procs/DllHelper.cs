using System.IO;
using System.Runtime.InteropServices;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Procs
{
    public static class DllHelper
    {
        private static readonly Dictionary<string, nint> LoadedDlls = new();

        public static unsafe delegate* unmanaged<int, byte**, byte*> ResolveDllTarget(
            DreamResourceManager resource,
            string dllName,
            string funcName)
        {
            var dll = GetDll(resource, dllName);

            if (!NativeLibrary.TryGetExport(dll, funcName, out var export))
                throw new MissingMethodException($"FFI: Unable to find symbol {funcName} in library {dllName}");

            return (delegate* unmanaged<int, byte**, byte*>)export;
        }

        private static nint GetDll(DreamResourceManager resource, string dllName)
        {
            if (LoadedDlls.TryGetValue(dllName, out var dll))
                return dll;

            if (!TryResolveDll(resource, dllName, out dll))
                throw new DllNotFoundException($"FFI: Unable to load {dllName}, unknown error."); //unknown because NativeLibrary doesn't give any error information.

            LoadedDlls.Add(dllName, dll);
            return dll;
        }

        private static bool TryResolveDll(DreamResourceManager resource, string dllName, out nint dll)
        {
            if (NativeLibrary.TryLoad(dllName, out dll))
                return true;

            // Simple load didn't pass, try next to dmb.
            if(!File.Exists(dllName))
                throw new DllNotFoundException($"FFI: Unable to load DLL {dllName}. Did you remember to build a 64-bit DLL instead of 32-bit?");
            return NativeLibrary.TryLoad(dllName, out dll);
        }
    }
}
