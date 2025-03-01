using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Exceptions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using YamlDotNet.Core;
using YamlDotNet.Core.Tokens;

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
    private static uint Byond_GetStrId(byte* cstr) {
        string? str = Marshal.PtrToStringUTF8((nint)cstr);
        if (str == null) {
            return NONE;
        }
        return _dreamManager?.FindString(str) ?? 0;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_AddGetStrId(byte* cstr) {
        string? str = Marshal.PtrToStringUTF8((nint)cstr);
        if (str == null) {
            return NONE;
        }
        return _dreamManager?.FindOrAddString(str) ?? 0;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVar(CByondValue* loc, byte* varname, CByondValue* result) {
        var reference = loc->data.@ref;

        string? varName = Marshal.PtrToStringUTF8((nint)varname);
        if (varName == null) {
            return 0;
        }

        DreamValue srcValue = _dreamManager.RefIdToValue((int)reference);
        switch (srcValue.Type) {
            default:
                return 0;
            case DreamValue.DreamValueType.DreamObject:
                var srcObj = srcValue.MustGetValueAsDreamObject();
                if (srcObj == null)
                    return 0;
                var srcVar = srcObj.GetVariable(varName);
                var cSrcVar = ValueToByondApi(srcVar);
                *result = cSrcVar;
                break;
        }
        return 1;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVarByStrId(CByondValue* loc, uint varname, CByondValue* result) {
        var reference = loc->data.@ref;

        DreamValue varNameVal = _dreamManager.RefIdToValue((int)varname);
        if (!varNameVal.TryGetValueAsString(out var varName)) return 0;

        DreamValue srcValue = _dreamManager.RefIdToValue((int)reference);
        switch (srcValue.Type) {
            default:
                return 0;
            case DreamValue.DreamValueType.DreamObject:
                var srcObj = srcValue.MustGetValueAsDreamObject();
                if (srcObj == null)
                    return 0;
                var srcVar = srcObj.GetVariable(varName);
                try {
                    var cSrcVar = ValueToByondApi(srcVar);
                    *result = cSrcVar;
                } catch (Exception e) {
                    return 0;
                }
                break;
        }
        return 1;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVar(CByondValue* loc, byte* varname, CByondValue* val) {
        var reference = loc->data.@ref;
        var cSrcType = val->type;
        var cSrcData = val->data;
        string? varName = Marshal.PtrToStringUTF8((nint)varname);
        if (varName == null) {
            return 0;
        }

        DreamValue srcValue;

        switch (cSrcType) {
            default:
            case ByondValueType.Null:
                srcValue = DreamValue.Null;
                break;
            case ByondValueType.Number:
                srcValue = new DreamValue(cSrcData.num);
                break;
        }

        DreamValue dstValue = _dreamManager.RefIdToValue((int)reference);
        switch (dstValue.Type) {
            default:
                return 0;
            case DreamValue.DreamValueType.DreamObject:
                var dstObj = dstValue.MustGetValueAsDreamObject();
                if (dstObj == null)
                    return 0;
                dstObj.SetVariable(varName, srcValue);
                break;
        }
        return 1;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVarByStrId(CByondValue* loc, uint varname, CByondValue* val) {
        var reference = loc->data.@ref;
        var cSrcType = val->type;
        var cSrcData = val->data;

        DreamValue varNameVal = _dreamManager.RefIdToValue((int)varname);
        if (!varNameVal.TryGetValueAsString(out var varName)) return 0;

        DreamValue srcValue;

        switch (cSrcType) {
            default:
            case ByondValueType.Null:
                srcValue = DreamValue.Null;
                break;
            case ByondValueType.Number:
                srcValue = new DreamValue(cSrcData.num);
                break;
        }

        DreamValue dstValue = _dreamManager.RefIdToValue((int)reference);
        switch (dstValue.Type) {
            default:
                return 0;
            case DreamValue.DreamValueType.DreamObject:
                var dstObj = dstValue.MustGetValueAsDreamObject();
                if (dstObj == null)
                    return 0;
                dstObj.SetVariable(varName, srcValue);
                break;
        }
        return 1;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CreateList(CByondValue* result) {
        var newList = _dreamManager.CreateList();
        DreamValue val = new DreamValue(newList);
        try {
            *result = ValueToByondApi(val);
        } catch(Exception e) {
            return 0;
        }
        return 1;
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
        int providedBufLen = (int)*buflen;
        var reference = src->data.@ref;
        DreamValue srcValue = _dreamManager.RefIdToValue((int)reference);

        if (!srcValue.TryGetValueAsString(out var str)) {
            *buflen = 0;
            return 0;
        }
        var utf8 = Encoding.UTF8.GetBytes(str);

        int length = utf8.Length;
        *buflen = (uint)length + 1;
        if (providedBufLen <= length) {
            return 0;
        }

        Marshal.Copy(utf8, 0, (nint)buf, length);
        buf[length] = 0;
        return 1;
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
