using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OpenDreamRuntime;

public static unsafe partial class ByondApi {
    private static void InitTrampoline() {
        var trampolines = new Trampolines {
            ByondValue_IsTrue = &ByondValue_IsTrue,
            ByondValue_Equals = &ByondValue_Equals,
            Byond_LastError = &Byond_LastError,
            Byond_GetVersion = &Byond_GetVersion,
            Byond_GetDMBVersion = &Byond_GetDMBVersion,
            Byond_ThreadSync = &Byond_ThreadSync,
            Byond_GetStrId = &Byond_GetStrId,
            Byond_AddGetStrId = &Byond_AddGetStrId,
            Byond_ReadVar = &Byond_ReadVar,
            Byond_ReadVarByStrId = &Byond_ReadVarByStrId,
            Byond_WriteVar = &Byond_WriteVar,
            Byond_WriteVarByStrId = &Byond_WriteVarByStrId,
            Byond_CreateList = &Byond_CreateList,
            Byond_ReadList = &Byond_ReadList,
            Byond_WriteList = &Byond_WriteList,
            Byond_ReadListAssoc = &Byond_ReadListAssoc,
            Byond_ReadListIndex = &Byond_ReadListIndex,
            Byond_WriteListIndex = &Byond_WriteListIndex,
            Byond_ReadPointer = &Byond_ReadPointer,
            Byond_WritePointer = &Byond_WritePointer,
            Byond_CallProc = &Byond_CallProc,
            Byond_CallProcByStrId = &Byond_CallProcByStrId,
            Byond_CallGlobalProc = &Byond_CallGlobalProc,
            Byond_CallGlobalProcByStrId = &Byond_CallGlobalProcByStrId,
            Byond_ToString = &Byond_ToString,
            Byond_Block = &Byond_Block,
            Byond_Length = &Byond_Length,
            Byond_LocateIn = &Byond_LocateIn,
            Byond_LocateXYZ = &Byond_LocateXYZ,
            Byond_New = &Byond_New,
            Byond_NewArglist = &Byond_NewArglist,
            Byond_Refcount = &Byond_Refcount,
            Byond_XYZ = &Byond_XYZ,
            Byond_PixLoc = &Byond_PixLoc,
            Byond_BoundPixLoc = &Byond_BoundPixLoc,
            ByondValue_IncRef = &ByondValue_IncRef,
            ByondValue_DecRef = &ByondValue_DecRef,
            ByondValue_DecTempRef = &ByondValue_DecTempRef,
            Byond_TestRef = &Byond_TestRef,
            OpenDream_Internal_SetCrash = &OpenDream_Internal_SetCrash,
        };

        NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
        OpenDream_Internal_Init(&trampolines);
    }

    [LibraryImport("byond")]
    private static partial void OpenDream_Internal_Init(Trampolines* trampolines);

    private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        if (libraryName == "byond") {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                return NativeLibrary.Load("byondcore", assembly, searchPath);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                // Need to load as RTLD_GLOBAL, otherwise byondapi-rs can't find the symbols.
                var attempt = dlopen("libbyond.so", RTLD_LAZY | RTLD_GLOBAL);
                if (attempt != 0)
                    return attempt;

                var path = Path.Combine(Path.GetDirectoryName(assembly.Location), "libbyond.so");
                attempt = dlopen(path, RTLD_LAZY | RTLD_GLOBAL);
                if (attempt != 0)
                    return attempt;
            }
        }

        // Otherwise, fallback to default import resolver.
        return IntPtr.Zero;
    }

    private const int RTLD_LAZY = 0x1;
    private const int RTLD_GLOBAL = 0x100;

    [LibraryImport("libc", StringMarshalling=StringMarshalling.Utf8)]
    private static partial nint dlopen(string filename, int flags);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "NotAccessedField.Local")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    private struct Trampolines {
        public delegate* unmanaged[Cdecl]<CByondValue*, byte> ByondValue_IsTrue;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, byte> ByondValue_Equals;
        public delegate* unmanaged[Cdecl]<byte*> Byond_LastError;
        public delegate* unmanaged[Cdecl]<uint*, uint*, void> Byond_GetVersion;
        public delegate* unmanaged[Cdecl]<uint> Byond_GetDMBVersion;
        public delegate* unmanaged[Cdecl]<delegate* unmanaged[Cdecl]<void*, CByondValue>, void*, byte, CByondValue> Byond_ThreadSync;
        public delegate* unmanaged[Cdecl]<byte*, uint> Byond_GetStrId;
        public delegate* unmanaged[Cdecl]<byte*, uint> Byond_AddGetStrId;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte*, CByondValue*, byte> Byond_ReadVar;
        public delegate* unmanaged[Cdecl]<CByondValue*, uint, CByondValue*, byte> Byond_ReadVarByStrId;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte*, CByondValue*, byte> Byond_WriteVar;
        public delegate* unmanaged[Cdecl]<CByondValue*, uint, CByondValue*, byte> Byond_WriteVarByStrId;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte> Byond_CreateList;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, uint*, byte> Byond_ReadList;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, uint, byte> Byond_WriteList;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, uint*, byte> Byond_ReadListAssoc;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, CByondValue*, byte> Byond_ReadListIndex;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, CByondValue*, byte> Byond_WriteListIndex;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, byte> Byond_ReadPointer;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, byte> Byond_WritePointer;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte*, CByondValue*, uint, CByondValue*, byte> Byond_CallProc;
        public delegate* unmanaged[Cdecl]<CByondValue*, uint, CByondValue*, uint, CByondValue*, byte> Byond_CallProcByStrId;
        public delegate* unmanaged[Cdecl]<byte*, CByondValue*, uint, CByondValue*, byte> Byond_CallGlobalProc;
        public delegate* unmanaged[Cdecl]<uint, CByondValue*, uint, CByondValue*, byte> Byond_CallGlobalProcByStrId;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte*, uint*, byte> Byond_ToString;
        public delegate* unmanaged[Cdecl]<CByondXYZ*, CByondXYZ*, CByondValue*, uint*, byte> Byond_Block;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, byte> Byond_Length;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, CByondValue*, byte> Byond_LocateIn;
        public delegate* unmanaged[Cdecl]<CByondXYZ*, CByondValue*, byte> Byond_LocateXYZ;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, uint, CByondValue*, byte> Byond_New;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondValue*, CByondValue*, byte> Byond_NewArglist;
        public delegate* unmanaged[Cdecl]<CByondValue*, uint*, byte> Byond_Refcount;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondXYZ*, byte> Byond_XYZ;
        public delegate* unmanaged[Cdecl]<CByondValue*, CByondPixLoc*, byte> Byond_PixLoc;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte, CByondPixLoc*, byte> Byond_BoundPixLoc;
        public delegate* unmanaged[Cdecl]<CByondValue*, void> ByondValue_IncRef;
        public delegate* unmanaged[Cdecl]<CByondValue*, void> ByondValue_DecRef;
        public delegate* unmanaged[Cdecl]<CByondValue*, void> ByondValue_DecTempRef;
        public delegate* unmanaged[Cdecl]<CByondValue*, byte> Byond_TestRef;
        public delegate* unmanaged[Cdecl]<byte*, void> OpenDream_Internal_SetCrash;
    }
}
