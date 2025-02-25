using System.IO;
using System.Runtime.InteropServices;
using OpenDreamRuntime.Resources;

namespace OpenDreamRuntime.Procs
{
    public static class DllHelper
    {
        private static readonly Dictionary<string, nint> LoadedDlls = new();

        public static unsafe delegate* unmanaged[Cdecl]<int, byte**, byte*> ResolveDllTarget(
            DreamResourceManager resource,
            string dllName,
            string funcName)
        {
            // stdcall convention
            if (funcName.Contains('@'))
                throw new NotSupportedException("Stdcall calling convention is not supported in OpenDream");

            var dll = GetDll(resource, dllName);

            if (!NativeLibrary.TryGetExport(dll, funcName, out var export))
                throw new MissingMethodException($"FFI: Unable to find symbol {funcName} in library {dllName}");

            return (delegate* unmanaged[Cdecl]<int, byte**, byte*>)export;
        }

        private static nint GetDll(DreamResourceManager resource, string dllName)
        {
            if (LoadedDlls.TryGetValue(dllName, out var dll))
                return dll;

            if (!TryResolveDll(resource, dllName, out dll))
                throw new DllNotFoundException($"FFI: Unable to load {dllName}, unknown error. Did you remember to build a 64-bit DLL instead of 32-bit?"); //unknown because NativeLibrary doesn't give any error information.

            LoadedDlls.Add(dllName, dll);
            return dll;
        }

        private static bool TryResolveDll(DreamResourceManager resource, string dllName, out nint dll)
        {
            if (NativeLibrary.TryLoad(dllName, out dll))
                return true;

            // Simple load didn't pass, try next to dmb.
            var root = resource.RootPath;
            var fullPath = Path.Combine(root, dllName);
            if(!File.Exists(fullPath))
                throw new DllNotFoundException($"FFI: Unable to load {dllName}. File not found at {fullPath}");
            return NativeLibrary.TryLoad(fullPath, out dll);
        }
    }
}
