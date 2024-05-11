using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace OpenDreamRuntime;

public static unsafe partial class ByondApi {
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* Byond_LastError() {
        return PinningIsNotReal("no error"u8);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Byond_GetVersion(uint* version, uint* build) {
        *version = 300;
        *build = 5;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_GetDMBVersion() {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static CByondValue Byond_ThreadSync(delegate* unmanaged[Cdecl]<void*, CByondValue> callback, void* data, byte block) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_GetStrId(byte* str) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_AddGetStrId(byte* str) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVar(CByondValue* loc, byte* varname, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVarByStrId(CByondValue* loc, uint varname, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVar(CByondValue* loc, byte* varname, CByondValue* val) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVarByStrId(CByondValue* loc, uint varname, CByondValue* val) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CreateList(CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadList(CByondValue* loc, CByondValue* list, uint* len) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteList(CByondValue* loc, CByondValue* list, uint len) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadListAssoc(CByondValue* loc, CByondValue* list, uint* len) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadListIndex(CByondValue* loc, CByondValue* idx, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteListIndex(CByondValue* loc, CByondValue* idx, CByondValue* val) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadPointer(CByondValue* ptr, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WritePointer(CByondValue* ptr, CByondValue* val) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallProc(CByondValue* src, byte* name, CByondValue* arg, uint arg_count, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallProcByStrId(CByondValue* src, uint name, CByondValue* arg, uint arg_count, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProc(byte* name, CByondValue* arg, uint arg_count, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProcByStrId(uint name, CByondValue* arg, uint arg_count, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ToString(CByondValue* src, byte* buf, uint* buflen) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Block(CByondXYZ* corner1, CByondXYZ* corner2, CByondValue* list, uint* len) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Length(CByondValue* src, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateIn(CByondValue* type, CByondValue* list, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateXYZ(CByondXYZ* xyz, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_New(CByondValue* type, CByondValue* arg, uint arg_count, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_NewArglist(CByondValue* type, CByondValue* arglist, CByondValue* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Refcount(CByondValue* src, uint* result) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_XYZ(CByondValue* src, CByondXYZ* xyz) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_IncRef(CByondValue* src) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_DecRef(CByondValue* src) {
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_TestRef(CByondValue* src) {
        throw new NotImplementedException();
    }
}
