using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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
        return 9001;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static CByondValue Byond_ThreadSync(delegate* unmanaged[Cdecl]<void*, CByondValue> callback, void* data, byte block) {
        if (callback == null || data == null) {
            return new CByondValue() { type = ByondValueType.Null, data = { @ref = 0 } };
        }
        throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Returns a reference to an existing string ID, but does not create a new string ID.
     * Blocks if not on the main thread.
     * @param str Null-terminated string
     * @return ID of string; NONE if string does not exist
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_GetStrId(byte* cstr) {
        if (cstr == null) {
            return NONE;
        }

        string? str = Marshal.PtrToStringUTF8((nint)cstr);
        if (str == null) {
            return NONE;
        }

        return _dreamManager!.FindString(str) ?? NONE;
    }

    /** byondapi.h comment:
     * Returns a reference to an existing string ID or creates a new string ID with a temporary reference.
     * Blocks if not on the main thread.
     * @param str Null-terminated string
     * @return ID of string; NONE if string creation failed
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint Byond_AddGetStrId(byte* cstr) {
        if (cstr == null) {
            return NONE;
        }

        string? str = Marshal.PtrToStringUTF8((nint)cstr);
        if (str == null) {
            return NONE;
        }

        return _dreamManager!.FindOrAddString(str);
    }

    /** byondapi.h comment:
     * Reads an object variable by name.
     * Blocks if not on the main thread.
     * @param loc Object that owns the var
     * @param varname Var name as null-terminated string
     * @param result Pointer to accept result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVar(CByondValue* loc, byte* varname, CByondValue* result) {
        if (loc == null || varname == null || result == null) {
            return 0;
        }

        try {
            string? varName = Marshal.PtrToStringUTF8((nint)varname);
            if (varName == null) {
                return 0;
            }

            DreamValue srcValue = ValueFromDreamApi(*loc);
            if (!srcValue.TryGetValueAsDreamObject(out var srcObj)) return 0;

            if (srcObj == null) return 0;

            var srcVar = srcObj.GetVariable(varName);
            var cSrcVar = ValueToByondApi(srcVar);
            *result = cSrcVar;
        } catch (Exception) {
             return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Reads an object variable by the string ID of its var name.
     * ID can be cached ahead of time for performance.
     * Blocks if not on the main thread.
     * @param loc Object that owns the var
     * @param varname Var name as string ID
     * @param result Pointer to accept result
     * @return True on success
     * @see Byond_GetStrId()
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadVarByStrId(CByondValue* loc, uint varname, CByondValue* result) {
        if (loc == null || result == null) {
            return 0;
        }

        try {
            DreamValue varNameVal = _dreamManager!.RefIdToValue((int)varname);
            if (!varNameVal.TryGetValueAsString(out var varName)) return 0;

            DreamValue srcValue = ValueFromDreamApi(*loc);
            if (!srcValue.TryGetValueAsDreamObject(out var srcObj)) return 0;
            if (srcObj == null) return 0;

            var srcVar = srcObj.GetVariable(varName);
            var cSrcVar = ValueToByondApi(srcVar);
            *result = cSrcVar;
        } catch (Exception) {
             return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Writes an object variable by name.
     * Blocks if not on the main thread.
     * @param loc Object that owns the var
     * @param varname Var name as null-terminated string
     * @param val New value
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVar(CByondValue* loc, byte* varname, CByondValue* val) {
        if (loc == null || varname == null || val == null) {
            return 0;
        }

        try {
            string? varName = Marshal.PtrToStringUTF8((nint)varname);
            if (varName == null) {
                return 0;
            }

            DreamValue srcValue = ValueFromDreamApi(*val);
            DreamValue dstValue = ValueFromDreamApi(*loc);
            if (!dstValue.TryGetValueAsDreamObject(out var dstObj)) return 0;
            if (dstObj == null) return 0;

            dstObj.SetVariable(varName, srcValue);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Writes an object variable by the string ID of its var name.
     * ID can be cached ahead of time for performance.
     * Blocks if not on the main thread.
     * @param loc Object that owns the var
     * @param varname Var name as string ID
     * @param val New value
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteVarByStrId(CByondValue* loc, uint varname, CByondValue* val) {
        try {
            DreamValue varNameVal = _dreamManager!.RefIdToValue((int)varname);
            if (!varNameVal.TryGetValueAsString(out var varName)) return 0;

            DreamValue srcValue = ValueFromDreamApi(*val);

            DreamValue dstValue = ValueFromDreamApi(*loc);
            if (!dstValue.TryGetValueAsDreamObject(out var dstObj)) return 0;
            if (dstObj == null) return 0;

            dstObj.SetVariable(varName, srcValue);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Creates an empty list with a temporary reference. Equivalent to list().
     * Blocks if not on the main thread.
     * @param result Result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CreateList(CByondValue* result) {
        var newList = _objectTree!.CreateList();
        DreamValue val = new DreamValue(newList);
        try {
            *result = ValueToByondApi(val);
        } catch(Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadList(CByondValue* loc, CByondValue* list, uint* len) {
        if (len == null) {
            return 0;
        }

        int providedBufLen = (int)*len;

        DreamValue srcValue = ValueFromDreamApi(*loc);
        if (!srcValue.TryGetValueAsDreamList(out var srcList)) {
            *len = 0;
            return 0;
        }

        var srcDreamVals = srcList.GetValues();
        int length = srcDreamVals.Count;
        *len = (uint)length;
        if (list == null || providedBufLen < length) {
            return 0;
        }

        try {
            for (int i = 0; i < length; i++) {
                list[i] = ValueToByondApi(srcDreamVals[i]);
            }
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteList(CByondValue* loc, CByondValue* list, uint len) {
        if (list == null || loc == null) {
            return 0;
        }

        try {
            DreamValue dstValue = ValueFromDreamApi(*loc);
            if (!dstValue.TryGetValueAsDreamList(out DreamList? dstListValue)) {
                return 0;
            }
            for (int i = 0; i < len; i++) {
                DreamValue srcValue = ValueFromDreamApi(list[i]);
                dstListValue.AddValue(srcValue);
            }
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadListAssoc(CByondValue* loc, CByondValue* list, uint* len) {
        if (len == null) {
            return 0;
        }

        int providedBufLen = (int)*len;

        DreamValue srcValue = ValueFromDreamApi(*loc);
        if (!srcValue.TryGetValueAsDreamList(out var srcList)) {
            *len = 0;
            return 0;
        }

        var srcDreamVals = srcList.GetAssociativeValues();
        int length = srcDreamVals.Count*2;
        *len = (uint)length;
        if (list == null || providedBufLen < length) {
            return 0;
        }

        try {
            int i = 0;
            foreach (var entry in srcDreamVals) {
                list[i] = ValueToByondApi(entry.Key);
                list[i+1] = ValueToByondApi(entry.Value);
                i += 2;
            }
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadListIndex(CByondValue* loc, CByondValue* cIdx, CByondValue* result) {
        if (loc == null || cIdx == null || result == null) {
            return 0;
        }

        try {
            DreamValue idx = ValueFromDreamApi(*cIdx);
            DreamValue listValue = ValueFromDreamApi(*loc);
            if (!listValue.TryGetValueAsDreamList(out var srcList)) {
                return 0;
            }

            var val = srcList.GetValue(idx);
            *result = ValueToByondApi(val);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteListIndex(CByondValue* loc, CByondValue* cIdx, CByondValue* cVal) {
        if (loc == null || cIdx == null || cVal == null) {
            return 0;
        }

        try {
            DreamValue idx = ValueFromDreamApi(*cIdx);
            DreamValue listValue = ValueFromDreamApi(*loc);
            if (!listValue.TryGetValueAsDreamList(out var dstList)) {
                return 0;
            }

            var val = ValueFromDreamApi(*cVal);
            dstList.SetValue(idx, val, true);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadPointer(CByondValue* cPtr, CByondValue* result) {
        if (cPtr == null || result == null) {
            return 0;
        }
        throw new NotImplementedException();
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WritePointer(CByondValue* cPtr, CByondValue* cVal) {
        if (cPtr == null || cVal == null) {
            return 0;
        }
        throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallProc(CByondValue* cSrc, byte* cName, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cSrc == null || cArgs == null || cResult == null) {
            return 0;
        }

        try {
            string? str = Marshal.PtrToStringUTF8((nint)cName);
            if (str == null) {
                return 0;
            }

            DreamValue src = ValueFromDreamApi(*cSrc);
            if (!src.TryGetValueAsDreamObject(out var srcObj)) return 0;

            if (srcObj == null) return 0;

            var srcVar = srcObj.GetVariable(str);
            if (!srcVar.TryGetValueAsProc(out var proc)) return 0;

            List<DreamValue> argList = new List<DreamValue>((int)arg_count);

            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList.Add(arg);
            }

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(argList));

            // Can we know the user?
            var result = proc.Spawn(srcObj, args);

            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallProcByStrId(CByondValue* cSrc, uint name, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cSrc == null || cArgs == null || cResult == null) {
            return 0;
        }

        try {
            DreamValue procNameVal = _dreamManager!.RefIdToValue((int)name);
            if (!procNameVal.TryGetValueAsString(out var procName)) return 0;

            DreamValue src = ValueFromDreamApi(*cSrc);
            if (!src.TryGetValueAsDreamObject(out var srcObj)) return 0;

            if (srcObj == null) return 0;

            var srcVar = srcObj.GetVariable(procName);
            if (!srcVar.TryGetValueAsProc(out var proc)) return 0;

            List<DreamValue> argList = new List<DreamValue>((int)arg_count);

            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList.Add(arg);
            }

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(argList));

            // Can we know the user?
            var result = proc.Spawn(srcObj, args);

            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProc(byte* cName, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cArgs == null || cResult == null) {
            return 0;
        }

        try {
            string? str = Marshal.PtrToStringUTF8((nint)cName);
            if (str == null) {
                return 0;
            }

            if (!_dreamManager!.TryGetGlobalProc(str, out var proc)) return 0;

            List<DreamValue> argList = new List<DreamValue>((int)arg_count);

            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList.Add(arg);
            }

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(argList));

            // src? usr?
            var result = proc.Spawn(null, args);

            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProcByStrId(uint cName, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cArgs == null || cResult == null) {
            return 0;
        }

        try {
            string? str = Marshal.PtrToStringUTF8((nint)cName);
            if (str == null) {
                return 0;
            }

            if (!_dreamManager!.TryGetGlobalProc(str, out var proc)) return 0;

            List<DreamValue> argList = new List<DreamValue>((int)arg_count);

            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList.Add(arg);
            }

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(argList));

            // src? usr?
            var result = proc.Spawn(null, args);

            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ToString(CByondValue* src, byte* buf, uint* buflen) {
        if (src == null || buf == null || buflen == null) {
            return 0;
        }

        try {
            int providedBufLen = (int)*buflen;
            DreamValue srcValue = ValueFromDreamApi(*src);

            if (!srcValue.TryGetValueAsString(out var str)) {
                *buflen = 0;
                return 0;
            }
            var utf8 = Encoding.UTF8.GetBytes(str);

            int length = utf8.Length;
            *buflen = (uint)length + 1;
            if (buf == null || providedBufLen <= length) {
                return 0;
            }

            Marshal.Copy(utf8, 0, (nint)buf, length);
            buf[length] = 0;
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Block(CByondXYZ* corner1, CByondXYZ* corner2, CByondValue* cList, uint* len) {
        if (corner1 == null || corner2 == null || cList == null || len == null) {
            return 0;
        }

        int minX = Math.Min(corner1->x, corner2->x);
        int minY = Math.Min(corner1->y, corner2->y);
        int minZ = Math.Min(corner1->z, corner2->z);

        int maxX = Math.Max(corner1->x, corner2->x);
        int maxY = Math.Max(corner1->y, corner2->y);
        int maxZ = Math.Max(corner1->z, corner2->z);

        List<CByondValue> list = new();
        try {
            for (int k = minZ; k <= maxZ; k++) {
                for (int j = minY; j <= maxY; j++) {
                    for (int i = minX; i <= maxX; i++) {
                        if (_dreamMapManager!.TryGetTurfAt(new Vector2i(i, j), k, out var turf)) {
                            DreamValue val = new(turf);
                            var cVal = ValueToByondApi(val);
                            list.Add(cVal);
                        }
                    }
                }
            }
        } catch (Exception) {
            return 0;
        }

        if (*len < list.Count) {
            *len = (uint)list.Count;
            return 0;
        }

        *len = (uint)list.Count;
        for (int i = 0; i < list.Count; i++) {
            cList[i] = list[i];
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Length(CByondValue* src, CByondValue* result) {
        *result = new CByondValue();
        result->type = ByondValueType.Number;
        if (src == null || result == null) {
            return 0;
        }

        DreamValue srcValue = ValueFromDreamApi(*src);
        try {
            switch (srcValue.Type) {
                default:
                    return 0;
                case DreamValue.DreamValueType.DreamObject:
                    if (srcValue.TryGetValueAsDreamList(out var list)) {
                        result->data.num = list.GetLength();
                    } else if (srcValue.TryGetValueAsDreamObject<DreamObjectVector>(out var vec)) {
                        result->data.num = vec.Size;
                    } else {
                        return 0;
                    }
                    break;
                case DreamValue.DreamValueType.String:
                    var s = srcValue.MustGetValueAsString();
                    result->data.num = s.Length;
                    break;
                case DreamValue.DreamValueType.DreamResource:
                    var r = srcValue.MustGetValueAsDreamResource();
                    if (r.ResourceData == null) {
                        // null file, so fail?
                        return 0;
                    }
                    result->data.num = r.ResourceData.Length;
                    break;
            }
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** <see cref="DMOpcodeHandlers.Locate(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateIn(CByondValue* type, CByondValue* list, CByondValue* result) {
        throw new NotImplementedException();
    }

    /** <see cref="DMOpcodeHandlers.LocateCoord(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateXYZ(CByondXYZ* xyz, CByondValue* result) {
        if (xyz == null || result == null) {
            return 1;
        }

        List<CByondValue> list = new();
        try {
            if (_dreamMapManager!.TryGetTurfAt(new Vector2i(xyz->x, xyz->y), xyz->z, out var turf)) {
                DreamValue val = new(turf);
                var cVal = ValueToByondApi(val);
                *result = cVal;
                return 1;
            } else {
                *result = ValueToByondApi(DreamValue.Null);
            }
        } catch (Exception) {
        }

        return 1;
    }

    /** <see cref="DMOpcodeHandlers.CreateObject(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_New(CByondValue* cType, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cType == null || cArgs == null || cResult == null) {
            return 0;
        }

        try {
            var typeVal = ValueFromDreamApi(*cType);
            if (!typeVal.TryGetValueAsType(out TreeEntry? type)) return 0;

            var objectDef = type.ObjectDefinition;
            var newProc = objectDef.GetProc("New");

            List<DreamValue> argList = new();
            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList.Add(arg);
            }

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(argList));

            if (objectDef.IsSubtypeOf(_objectTree!.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = args.GetArgument(0);
                if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
                    //ThrowInvalidTurfLoc(loc);
                    return 0;
                }

                _dreamMapManager!.SetTurf(turf, objectDef, args);
                return 1;
            }

            var newObject = _objectTree!.CreateObject(type);
            // call new
            var result = newProc.Spawn(newObject, args);
            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }
        return 1;
    }

    /** <see cref="DMOpcodeHandlers.CreateObject(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_NewArglist(CByondValue* cType, CByondValue* cArglist, CByondValue* cResult) {
        if (cType == null || cArglist == null || cResult == null) {
            return 0;
        }

        try {
            var typeVal = ValueFromDreamApi(*cType);
            if (!typeVal.TryGetValueAsType(out var type)) return 0;

            var objectDef = type.ObjectDefinition;
            var newProc = objectDef.GetProc("New");

            var arglistVal = ValueFromDreamApi(*cArglist);
            if (!arglistVal.TryGetValueAsDreamList(out DreamList? arglist)) return 0;

            var args = new DreamProcArguments(CollectionsMarshal.AsSpan(arglist.GetValues()));

            if (objectDef.IsSubtypeOf(_objectTree!.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = args.GetArgument(0);
                if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
                    //ThrowInvalidTurfLoc(loc);
                    return 0;
                }

                _dreamMapManager!.SetTurf(turf, objectDef, args);
                return 1;
            }

            var newObject = _objectTree!.CreateObject(type);
            // call new
            var result = newProc.Spawn(newObject, args);
            *cResult = ValueToByondApi(result);
        } catch (Exception) {
            return 0;
        }
        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Refcount(CByondValue* src, uint* result) {
        if (src == null || result == null) return 0;

        // woah that's a lot of refs
        // i wonder if it's true??
        *result = 100;
        // (it's not)

        return 1;
    }


    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_XYZ(CByondValue* src, CByondXYZ* xyz) {
        if (src == null || xyz == null) return 0;
        *xyz = new CByondXYZ();

        try {
            var srcVal = ValueFromDreamApi(*src);
            if (!srcVal.TryGetValueAsDreamObject<DreamObjectAtom>(out var srcObj)) return 0;
            try {
                // byondapi.h mentions an off-map check. Should probably do something like that.

                // certainly a better way than this...
                var xObj = srcObj.GetVariable("X");
                var x = xObj.MustGetValueAsInteger();
                var yObj = srcObj.GetVariable("Y");
                var y = yObj.MustGetValueAsInteger();
                var zObj = srcObj.GetVariable("Z");
                var z = zObj.MustGetValueAsInteger();

                xyz->x = (short)x;
                xyz->y = (short)y;
                xyz->z = (short)z;
            } catch (Exception) {
                return 1;
            }
        } catch (Exception) {
                return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_IncRef(CByondValue* src) {
        if (src == null) return;
        throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_DecRef(CByondValue* src) {
        if (src == null) return;
        throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_TestRef(CByondValue* src) {
        if (src == null) return 0;
        if (src->type == ByondValueType.Null) {
            return 0;
        }

        var srcValue = _dreamManager!.RefIdToValue((int)src->data.@ref);

        return srcValue == DreamValue.Null ? (byte)0 : (byte)1;
    }
}
