using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using OpenDreamRuntime.Procs;
using OpenDreamRuntime.Procs.Native;

// ReSharper disable InconsistentNaming

namespace OpenDreamRuntime.ByondApi;

public static unsafe partial class ByondApi {
    public const uint NONE = 0xFFFF;

    /** byondapi.h comment:
     * Determines if a value is logically true or false
     *
     * @param v Pointer to CByondValue
     * @return Truthiness of value
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte ByondValue_IsTrue(CByondValue* v) {
        return ValueFromDreamApi(*v).IsTruthy() ? (byte)1 : (byte)0;
    }

    /** byondapi.h comment:
     * Compares two values for equality
     * @param a Pointer to CByondValue
     * @param b Pointer to CByondValue
     * @return True if values are equal
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte ByondValue_Equals(CByondValue* a, CByondValue* b) {
        var left = ValueFromDreamApi(*a);
        var right = ValueFromDreamApi(*b);

        return DMOpcodeHandlers.IsEqual(left, right) ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* Byond_LastError() {
        return null;
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
        if (callback == null! || data == null) {
            return new CByondValue { type = ByondValueType.Null, data = { @ref = 0 } };
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

        var strId = _dreamManager!.FindString(str);
        if (strId != null) {
            return (uint)RefType.String | strId.Value;
        } else {
            return NONE;
        }
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

        var strIdx = _dreamManager!.FindOrAddString(str);
        return (uint)RefType.String | strIdx;
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

    /** byondapi.h comment:
     * Reads items from a list.
     * Blocks if not on the main thread.
     * @param loc The list to read
     * @param list CByondValue array, allocated by caller (can be null if querying length)
     * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
     * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
     */
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

    /** byondapi.h comment:
     * Writes items to a list, in place of old contents.
     * Blocks if not on the main thread.
     * @param loc The list to fill
     * @param list CByondValue array of items to write
     * @param len Number of items to write
     * @return True on success
     */
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

            dstListValue.Cut();
            for (int i = 0; i < len; i++) {
                DreamValue srcValue = ValueFromDreamApi(list[i]);
                dstListValue.AddValue(srcValue);
            }
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Reads items as key,value pairs from an associative list, storing them sequentially as key1, value1, key2, value2, etc.
     * Blocks if not on the main thread.
     * @param loc The list to read
     * @param list CByondValue array, allocated by caller (can be null if querying length)
     * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
     * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
     */
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

    /** byondapi.h comment:
     * Reads an item from a list.
     * Blocks if not on the main thread.
     * @param loc The list
     * @param idx The index in the list (may be a number, or a non-number if using associative lists)
     * @param result Pointer to accept result
     * @return True on success
     */
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

    private static byte CallProcShared(DreamObject? src, DreamProc proc, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        DreamValue[] argList = new DreamValue[arg_count];

        for (int i = 0; i < arg_count; i++) {
            var arg = ValueFromDreamApi(cArgs[i]);
            argList[i] = arg;
        }

        var args = new DreamProcArguments(argList);

        // TODO
        // Can we know the user?
        var result = proc.Spawn(src, args);

        *cResult = ValueToByondApi(result);
        return 1;
    }

    // TODO: make sure return happens immediately if the callee sleeps
    /** byondapi.h comment:
     * Calls an object proc by name.
     * The proc call is treated as waitfor=0 and will return immediately on sleep.
     * Blocks if not on the main thread.
     * @param src The object that owns the proc
     * @param name Proc name as null-terminated string
     * @param arg Array of arguments
     * @param arg_count Number of arguments
     * @param result Pointer to accept result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallProc(CByondValue* cSrc, byte* cName, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cSrc == null || cArgs == null || cResult == null) {
            return 0;
        }

        try {
            string? str = Marshal.PtrToStringUTF8((nint)cName);
            if (str == null) return 0;

            DreamValue src = ValueFromDreamApi(*cSrc);
            if (!src.TryGetValueAsDreamObject(out var srcObj)) return 0;
            if (srcObj == null) return 0;
            if (!srcObj.TryGetProc(str, out var proc)) return 0;

            return CallProcShared(srcObj, proc, cArgs, arg_count, cResult);
        } catch (Exception) {
            return 0;
        }
    }

    /** byondapi.h comment:
     * Calls an object proc by name, where the name is a string ID.
     * The proc call is treated as waitfor=0 and will return immediately on sleep.
     * Blocks if not on the main thread.
     * @param src The object that owns the proc
     * @param name Proc name as string ID
     * @param arg Array of arguments
     * @param arg_count Number of arguments
     * @param result Pointer to accept result
     * @return True on success
     * @see Byond_GetStrId()
     */
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
            if (!srcObj.TryGetProc(procName, out var proc)) return 0;

            return CallProcShared(srcObj, proc, cArgs, arg_count, cResult);
        } catch (Exception) {
            return 0;
        }
    }

    /** byondapi.h comment:
     * Calls a global proc by name.
     * The proc call is treated as waitfor=0 and will return immediately on sleep.
     * Blocks if not on the main thread.
     * @param name Proc name as null-terminated string
     * @param arg Array of arguments
     * @param arg_count  Number of arguments
     * @param result Pointer to accept result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProc(byte* cName, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cArgs == null || cResult == null) {
            return 0;
        }

        try {
            string? str = Marshal.PtrToStringUTF8((nint)cName);
            if (str == null) return 0;
            if (!_dreamManager!.TryGetGlobalProc(str, out var proc)) return 0;

            CallProcShared(null, proc, cArgs, arg_count, cResult);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Calls a global proc by name, where the name is a string ID.
     * The proc call is treated as waitfor=0 and will return immediately on sleep.
     * Blocks if not on the main thread.
     * @param name Proc name as string ID
     * @param arg Array of arguments
     * @param arg_count Number of arguments
     * @param result Pointer to accept result
     * @return True on success
     * @see Byond_GetStrId()
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CallGlobalProcByStrId(uint name, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cArgs == null || cResult == null) {
            return 0;
        }

        try {
            DreamValue procNameVal = _dreamManager!.RefIdToValue((int)name);
            if (!procNameVal.TryGetValueAsString(out var procName)) return 0;
            if (!_dreamManager.TryGetGlobalProc(procName, out var proc)) return 0;

            CallProcShared(null, proc, cArgs, arg_count, cResult);
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Uses BYOND's internals to represent a value as text
     * Blocks if not on the main thread.
     * @param src The value to convert to text
     * @param buf char array, allocated by caller (can be null if querying length)
     * @param buflen Pointer to length of array in bytes; receives the string length (including trailing null) on success, or required length of array if not big enough
     * @return True on success; false with *buflen=0 for failure; false with *buflen=required size if array is not big enough
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ToString(CByondValue* src, byte* buf, uint* buflen) {
        if (src == null || buflen == null) {
            return 0;
        }

        try {
            int providedBufLen = (int)*buflen;
            DreamValue srcValue = ValueFromDreamApi(*src);
            var str = srcValue.Stringify();
            var utf8 = Encoding.UTF8.GetBytes(str);
            int length = utf8.Length;

            *buflen = (uint)length + 1;
            if (buf == null || providedBufLen <= length) {
                return 0;
            }

            Marshal.Copy(utf8, 0, (nint)buf, length);
            buf[length] = 0;
        } catch (Exception) {
            *buflen = 0;
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Equivalent to calling block(x1,y1,z1, x2,y2,z2).
     * Blocks if not on the main thread.
     * @param corner1 One corner of the block
     * @param corner2 Another corner of the block
     * @param list CByondValue array, allocated by caller (can be null if querying length)
     * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
     * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Block(CByondXYZ* corner1, CByondXYZ* corner2, CByondValue* cList, uint* len) {
        if (corner1 == null || corner2 == null || cList == null || len == null) {
            return 0;
        }

        List<CByondValue> list = new();
        try {
            var turfs = DreamProcNativeRoot.Block(_objectTree!, _dreamMapManager!,
                corner1->x, corner1->y, corner1->z,
                corner2->x, corner2->y, corner2->z);

            foreach (var turf in turfs.GetValues()) {
                list.Add(ValueToByondApi(turf));
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

    /** byondapi.h comment:
     * Equivalent to calling length(value).
     * Blocks if not on the main thread.
     * @param src The value
     * @param result Pointer to accept result as a CByondValue (intended for future possible override of length)
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Length(CByondValue* src, CByondValue* result) {
        if (src == null || result == null) {
            return 0;
        }

        DreamValue srcValue = ValueFromDreamApi(*src);
        try {
            *result = ValueToByondApi(DreamProcNativeRoot._length(srcValue, true));
            return 1;
        } catch (Exception) {
            return 0;
        }
    }

    /** <see cref="DMOpcodeHandlers.Locate(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateIn(CByondValue* type, CByondValue* list, CByondValue* result) {
        throw new NotImplementedException();
    }

    /** byondapi.h comment:
    * Equivalent to calling locate(x,y,z)
    * Blocks if not on the main thread.
    * Result is null if coords are invalid.
    * @param xyz The x,y,z coords
    * @param result Pointer to accept result
    * @return True (always)
    */
    /** <see cref="DMOpcodeHandlers.LocateCoord(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_LocateXYZ(CByondXYZ* xyz, CByondValue* result) {
        if (xyz == null || result == null) {
            return 1;
        }

        try {
            if (_dreamMapManager!.TryGetTurfAt(new Vector2i(xyz->x, xyz->y), xyz->z, out var turf)) {
                DreamValue val = new(turf);
                *result = ValueToByondApi(val);
            } else {
                *result = ValueToByondApi(DreamValue.Null);
            }
        } catch (Exception) {
            // ignored
        }

        return 1;
    }

    /** byondapi.h comment:
     * Equivalent to calling new type(...)
     * Blocks if not on the main thread.
     * @param type The type to create (type path or string)
     * @param arg Array of arguments
     * @param arg_count Number of arguments
     * @param result Pointer to accept result
     * @return True on success
     */
    /** <see cref="DMOpcodeHandlers.CreateObject(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_New(CByondValue* cType, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        if (cType == null || cArgs == null || cResult == null) {
            return 0;
        }

        try {
            var typeVal = ValueFromDreamApi(*cType);
            if (!typeVal.TryGetValueAsType(out var treeEntry)) {
                if (typeVal.TryGetValueAsString(out var pathString)) {
                    if (!_objectTree!.TryGetTreeEntry(pathString, out treeEntry))
                        return 0;
                } else {
                    return 0;
                }
            }

            var objectDef = treeEntry.ObjectDefinition;
            var argList = new DreamValue[arg_count];
            for (int i = 0; i < arg_count; i++) {
                var arg = ValueFromDreamApi(cArgs[i]);
                argList[i] = arg;
            }

            var args = new DreamProcArguments(argList);

            // TODO: This is code duplicated with DMOpcodeHandlers.CreateObject()
            if (objectDef.IsSubtypeOf(_objectTree!.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = args.GetArgument(0);
                if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
                    return 0;
                }

                _dreamMapManager!.SetTurf(turf, objectDef, args);
                return 1;
            }

            var newObject = _objectTree.CreateObject(treeEntry);
            newObject.InitSpawn(args);
            *cResult = ValueToByondApi(new DreamValue(newObject));
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Equivalent to calling new type(arglist)
     * Blocks if not on the main thread.
     * @param type The type to create (type path or string)
     * @param arglist Arguments, as a reference to an arglist
     * @param result Pointer to accept result
     * @return True on success
     */
    /** <see cref="DMOpcodeHandlers.CreateObject(DMProcState)"/> */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_NewArglist(CByondValue* cType, CByondValue* cArglist, CByondValue* cResult) {
        if (cType == null || cArglist == null || cResult == null) {
            return 0;
        }

        try {
            var typeVal = ValueFromDreamApi(*cType);
            if (!typeVal.TryGetValueAsType(out var treeEntry)) {
                if (typeVal.TryGetValueAsString(out var pathString)) {
                    if (!_objectTree!.TryGetTreeEntry(pathString, out treeEntry))
                        return 0;
                } else {
                    return 0;
                }
            }

            var objectDef = treeEntry.ObjectDefinition;

            var arglistVal = ValueFromDreamApi(*cArglist);
            if (!arglistVal.TryGetValueAsDreamList(out DreamList? arglist)) return 0;

            // Copy the arglist's values to a new array to ensure no shenanigans
            var argListValues = arglist.GetValues();
            var argValues = new DreamValue[argListValues.Count];
            for (int i = 0; i < argListValues.Count; i++) {
                argValues[i] = argListValues[i];
            }

            var args = new DreamProcArguments(argValues);

            // TODO: This is code duplicated with DMOpcodeHandlers.CreateObject()
            if (objectDef.IsSubtypeOf(_objectTree!.Turf)) {
                // Turfs are special. They're never created outside of map initialization
                // So instead this will replace an existing turf's type and return that same turf
                DreamValue loc = args.GetArgument(0);
                if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
                    return 0;
                }

                _dreamMapManager!.SetTurf(turf, objectDef, args);
                return 1;
            }

            var newObject = _objectTree.CreateObject(treeEntry);
            newObject.InitSpawn(args);
            *cResult = ValueToByondApi(new DreamValue(newObject));
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Refcount(CByondValue* src, uint* result) {
        if (src == null || result == null) return 0;

        // TODO
        // woah that's a lot of refs
        // i wonder if it's true??
        *result = 100;
        // (it's not)

        return 1;
    }

    /** byondapi.h comment:
     * Get x,y,z coords of an atom
     * Blocks if not on the main thread.
     * @param src The object to read
     * @param xyz Pointer to accept CByondXYZ result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_XYZ(CByondValue* src, CByondXYZ* xyz) {
        if (src == null || xyz == null) return 0;
        *xyz = new CByondXYZ();

        try {
            var srcVal = ValueFromDreamApi(*src);
            if (!srcVal.TryGetValueAsDreamObject<DreamObjectAtom>(out var srcObj)) return 0;

            var (x, y, z) = _atomManager!.GetAtomPosition(srcObj);
            xyz->x = (short)x;
            xyz->y = (short)y;
            xyz->z = (short)z;
        } catch (Exception) {
            return 0;
        }

        return 1;
    }

    /** byondapi.h comment:
     * Get pixloc coords of an atom
     * Blocks if not on the main thread.
     * @param src The object to read
     * @param pixloc Pointer to accept CByondPixLoc result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_PixLoc(CByondValue* src, CByondPixLoc *pixLoc) {
        if (src == null) return 0;
        throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Get pixloc coords of an atom based on its bounding box
     * Blocks if not on the main thread.
     * @param src The object to read
     * @param dir The direction
     * @param pixloc Pointer to accept CByondPixLoc result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_BoundPixLoc(CByondValue* src, byte dir, CByondPixLoc* pixLoc) {
        if (src == null) return 0;
        throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_IncRef(CByondValue* src) {
        //if (src == null) return;
        //throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_DecRef(CByondValue* src) {
        //if (src == null) return;
        //throw new NotImplementedException();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_DecTempRef(CByondValue* src) {
        //if (src == null) return;
        //throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Test if a reference-type CByondValue is valid
     * Blocks if not on the main thread.
     * @param src Pointer to the reference to test; will be filled with null if the reference is invalid
     * @return True if ref is valid; false if not
     */
    // Returns true if the ref is valid.
    // Returns false if the ref was not valid and had to be changed to null.
    // This only applies to ref types, not null/num/string which are always valid.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_TestRef(CByondValue* src) {
        if (src == null) return 0;
        if (src->type == ByondValueType.Null) {
            return 1;
        }

        var srcValue = _dreamManager!.RefIdToValue((int)src->data.@ref);

        if (srcValue == DreamValue.Null) {
            src->type = 0;
            src->data.@ref = 0;
            return 0;
        }

        return 1;
    }
}
