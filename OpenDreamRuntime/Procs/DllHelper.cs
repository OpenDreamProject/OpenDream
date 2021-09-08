using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenDreamRuntime.Procs
{
    public static class DllHelper
    {
        private static readonly Dictionary<string, nint> LoadedDlls = new();

        public static unsafe delegate* unmanaged<int, byte**, byte*> ResolveDllTarget(
            DreamRuntime runtime,
            string dllName,
            string funcName)
        {
            var dll = GetDll(runtime, dllName);

            if (!NativeLibrary.TryGetExport(dll, funcName, out var export))
                throw new MissingMethodException($"FFI: Unable to find symbol {export} in library {dllName}");

            return (delegate* unmanaged<int, byte**, byte*>)export;
        }

        private static nint GetDll(DreamRuntime runtime, string dllName)
        {
            if (LoadedDlls.TryGetValue(dllName, out var dll))
                return dll;

            if (!TryResolveDll(runtime, dllName, out dll))
                throw new DllNotFoundException($"FFI: Unable to load {dllName}");

            LoadedDlls.Add(dllName, dll);
            return dll;
        }

        private static bool TryResolveDll(DreamRuntime runtime, string dllName, out nint dll)
        {
            if (NativeLibrary.TryLoad(dllName, out dll))
                return true;

            // Simple load didn't pass, try next to dmb.
            var root = runtime.ResourceManager.RootPath;
            var fullPath = Path.Combine(root, dllName);

            return NativeLibrary.TryLoad(fullPath, out dll);
        }
    }
}
