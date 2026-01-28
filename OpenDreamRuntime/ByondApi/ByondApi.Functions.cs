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
    private const uint NONE = 0xFFFF;

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
        try {
            var utf8 = Encoding.UTF8.GetBytes(_lastError);
            var buf = (byte*)_lastErrorPtr;
            var copyLen = Math.Min(utf8.Length, LastErrorMaxLength - 1);

            Marshal.Copy(utf8, 0, (nint)buf, utf8.Length);
            buf[copyLen] = 0;

            // This does return a string that could be overwritten later by another call to Byond_LastError()
            // Don't know if BYOND does the same, but this was also made into a saner method in 516.1674 that we'll be updating this to later
            // So whatever
            return buf;
        } catch (Exception e) {
            SetLastError(e.Message); // Ironic. Maybe undesired?
            return null;
        }
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

    /** byondapi.h comment:
     * Runs a function as a callback on the main thread (or right away if already there)
     * All references created from Byondapi calls within your callback are persistent, not temporary, even though your callback runs on the main thread.
     * Blocking is optional. If already on the main thread, the block parameter is meaningless.
     * @param callback Function pointer to CByondValue function(void*)
     * @param data Void pointer (argument to function)
     * @param block True if this call should block while waiting for the callback to finish; false if not
     * @return CByondValue returned by the function (if it blocked; null if not)
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static CByondValue Byond_ThreadSync(delegate* unmanaged[Cdecl]<void*, CByondValue> callback, void* data, byte block) {
        if (callback == null! || data == null) {
            return new CByondValue { type = ByondValueType.Null, data = { @ref = 0 } };
        }

        if (block > 0) {
            return RunOnMainThread(() => callback(data));
        }

        RunOnMainThreadNonBlocking(() => callback(data));
        return ValueToByondApi(DreamValue.Null);
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

        return RunOnMainThread(() => {
            var strId = _dreamManager!.FindString(str);
            if (strId != null) {
                return (uint)RefType.String | strId.Value;
            } else {
                return NONE;
            }
        });
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

        return RunOnMainThread(() => {
            var strIdx = _dreamManager!.FindOrAddString(str);
            return (uint)RefType.String | strIdx;
        });
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
        if (loc == null || varname == null || result == null)
            return SetLastError("loc, varname, or result argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                string? varName = Marshal.PtrToStringUTF8((nint)varname);
                if (varName == null)
                    return SetLastError("varname argument was a null pointer");

                DreamValue srcValue = ValueFromDreamApi(*loc);
                if (!srcValue.TryGetValueAsDreamObject(out var srcObj))
                    return SetLastError("loc was not a DreamObject");
                if (srcObj == null)
                    return SetLastError("loc was null");

                var srcVar = srcObj.GetVariable(varName);
                var cSrcVar = ValueToByondApi(srcVar);
                *result = cSrcVar;
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (loc == null || result == null)
            return SetLastError("loc or result argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                DreamValue varNameVal = _dreamManager!.RefIdToValue((int)varname);
                if (!varNameVal.TryGetValueAsString(out var varName))
                    return SetLastError("varname argument was not a reference to a string");

                DreamValue srcValue = ValueFromDreamApi(*loc);
                if (!srcValue.TryGetValueAsDreamObject(out var srcObj))
                    return SetLastError("loc argument was not a DreamObject");
                if (srcObj == null)
                    return SetLastError("loc argument was null");

                var srcVar = srcObj.GetVariable(varName);
                var cSrcVar = ValueToByondApi(srcVar);
                *result = cSrcVar;
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (loc == null || varname == null || val == null)
            return SetLastError("loc, varname, or val argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                string? varName = Marshal.PtrToStringUTF8((nint)varname);
                if (varName == null)
                    return SetLastError("varname was a null pointer");

                DreamValue srcValue = ValueFromDreamApi(*val);
                DreamValue dstValue = ValueFromDreamApi(*loc);
                if (!dstValue.TryGetValueAsDreamObject(out var dstObj))
                    return SetLastError("loc argument was not a DreamObject");
                if (dstObj == null)
                    return SetLastError("loc argument was null");

                dstObj.SetVariable(varName, srcValue);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        return RunOnMainThread<byte>(() => {
            try {
                DreamValue varNameVal = _dreamManager!.RefIdToValue((int)varname);
                if (!varNameVal.TryGetValueAsString(out var varName))
                    return SetLastError("varname argument was not a ref to a string");

                DreamValue srcValue = ValueFromDreamApi(*val);
                DreamValue dstValue = ValueFromDreamApi(*loc);
                if (!dstValue.TryGetValueAsDreamObject(out var dstObj))
                    return SetLastError("loc argument was not a DreamObject");
                if (dstObj == null)
                    return SetLastError("loc argument was null");

                dstObj.SetVariable(varName, srcValue);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
    }

    /** byondapi.h comment:
     * Creates an empty list with a temporary reference. Equivalent to list().
     * Blocks if not on the main thread.
     * @param result Result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_CreateList(CByondValue* result) {
        return RunOnMainThread<byte>(() => {
            var newList = _objectTree!.CreateList();
            DreamValue val = new DreamValue(newList);
            try {
                *result = ValueToByondApi(val);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (len == null)
            return SetLastError("len argument was a null pointer");

        int providedBufLen = (int)*len;

        return RunOnMainThread<byte>(() => {
            DreamValue srcValue = ValueFromDreamApi(*loc);
            if (!srcValue.TryGetValueAsDreamList(out var srcList)) {
                *len = 0;
                return SetLastError("loc argument was not a list");
            }

            int length = srcList.GetLength();
            *len = (uint)length;
            if (list == null)
                return SetLastError("list argument was a null pointer");
            if (providedBufLen < length)
                return SetLastError($"provided buf length of {providedBufLen} was less than needed {length}");

            try {
                int i = 0;
                foreach (var value in srcList.EnumerateValues()) {
                    if (i >= length)
                        throw new Exception($"List {srcList} had more elements than the expected {length}");

                    list[i++] = ValueToByondApi(value);
                }
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (list == null || loc == null)
            return SetLastError("list or loc argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                DreamValue dstValue = ValueFromDreamApi(*loc);
                if (!dstValue.TryGetValueAsDreamList(out DreamList? dstListValue))
                    return SetLastError("loc argument was not a list");

                dstListValue.Cut();
                for (int i = 0; i < len; i++) {
                    DreamValue srcValue = ValueFromDreamApi(list[i]);
                    dstListValue.AddValue(srcValue);
                }
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (len == null)
            return SetLastError("len argument was a null pointer");

        int providedBufLen = (int)*len;

        return RunOnMainThread<byte>(() => {
            DreamValue srcValue = ValueFromDreamApi(*loc);
            if (!srcValue.TryGetValueAsDreamList(out var srcList)) {
                *len = 0;
                return SetLastError("loc argument was not a list");
            }

            var srcDreamVals = srcList.GetAssociativeValues();
            int length = srcDreamVals.Count * 2;
            *len = (uint)length;
            if (list == null)
                return SetLastError("list argument was a null pointer");
            if (providedBufLen < length)
                return SetLastError($"provided buf length of {providedBufLen} was less than needed {length}");

            try {
                int i = 0;
                foreach (var entry in srcDreamVals) {
                    list[i] = ValueToByondApi(entry.Key);
                    list[i + 1] = ValueToByondApi(entry.Value);
                    i += 2;
                }
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (loc == null || cIdx == null || result == null)
            return SetLastError("loc, cIdx, or result argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                DreamValue idx = ValueFromDreamApi(*cIdx);
                DreamValue listValue = ValueFromDreamApi(*loc);
                if (!listValue.TryGetValueAsDreamList(out var srcList))
                    return SetLastError("loc argument was not a list");

                var val = srcList.GetValue(idx);
                *result = ValueToByondApi(val);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
    }

    /** byondapi.h comment:
     * Writes an item to a list.
     * Blocks if not on the main thread.
     * @param loc The list
     * @param idx The index in the list (may be a number, or a non-number if using associative lists)
     * @param val New value
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WriteListIndex(CByondValue* loc, CByondValue* cIdx, CByondValue* cVal) {
        if (loc == null || cIdx == null || cVal == null)
            return SetLastError("loc, cIdx, or cVal argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                DreamValue idx = ValueFromDreamApi(*cIdx);
                DreamValue listValue = ValueFromDreamApi(*loc);
                if (!listValue.TryGetValueAsDreamList(out var dstList))
                    return SetLastError("loc argument was not a list");

                var val = ValueFromDreamApi(*cVal);
                dstList.SetValue(idx, val, true);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
    }

    /** byondapi.h comment:
     * Reads from a BYOND pointer
     * Blocks if not on the main thread.
     * @param ptr The BYOND pointer
     * @param result Pointer to accept result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_ReadPointer(CByondValue* cPtr, CByondValue* result) {
        if (cPtr == null || result == null)
            return SetLastError("cPtr or result argument was a null pointer");

        throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Writes to a BYOND pointer
     * Blocks if not on the main thread.
     * @param ptr The BYOND pointer
     * @param val New value
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_WritePointer(CByondValue* cPtr, CByondValue* cVal) {
        if (cPtr == null || cVal == null)
            return SetLastError("cPtr or cVal argument was a null pointer");

        throw new NotImplementedException();
    }

    private static byte CallProcShared(DreamObject? src, DreamProc proc, CByondValue* cArgs, uint arg_count, CByondValue* cResult) {
        DreamValue[] argList = new DreamValue[arg_count];

        for (int i = 0; i < arg_count; i++) {
            var arg = ValueFromDreamApi(cArgs[i]);
            argList[i] = arg;
        }

        var args = new DreamProcArguments(argList);

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
        if (cSrc == null || cArgs == null || cResult == null)
            return SetLastError("cSrc, cArgs, or cResult argument was a null pointer");

        return RunOnMainThread(() => {
            try {
                string? str = Marshal.PtrToStringUTF8((nint)cName);
                if (str == null)
                    return SetLastError("cName argument was a null pointer");

                DreamValue src = ValueFromDreamApi(*cSrc);
                if (!src.TryGetValueAsDreamObject(out var srcObj))
                    return SetLastError("cSrc argument was not a DreamObject");
                if (srcObj == null)
                    return SetLastError("cSrc argument was null");
                if (!srcObj.TryGetProc(str, out var proc))
                    return SetLastError($"cSrc argument does not own a proc named \"{str}\"");

                return CallProcShared(srcObj, proc, cArgs, arg_count, cResult);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }
        });
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
        if (cSrc == null || cArgs == null || cResult == null)
            return SetLastError("cSrc, cArgs, or cResult argument was a null pointer");

        return RunOnMainThread(() => {
            try {
                DreamValue procNameVal = _dreamManager!.RefIdToValue((int)name);
                if (!procNameVal.TryGetValueAsString(out var procName))
                    return SetLastError("name argument was not a ref to a string");

                DreamValue src = ValueFromDreamApi(*cSrc);
                if (!src.TryGetValueAsDreamObject(out var srcObj))
                    return SetLastError("cSrc argument was not a DreamObject");
                if (srcObj == null)
                    return SetLastError("cSrc argument was null");
                if (!srcObj.TryGetProc(procName, out var proc))
                    return SetLastError($"cSrc does not own a proc named \"{procName}\"");

                return CallProcShared(srcObj, proc, cArgs, arg_count, cResult);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }
        });
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
        if (cArgs == null || cResult == null)
            return SetLastError("cArgs or cResult argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                string? str = Marshal.PtrToStringUTF8((nint)cName);
                if (str == null)
                    return SetLastError("cName argument was a null pointer");
                if (!_dreamManager!.TryGetGlobalProc(str, out var proc))
                    return SetLastError($"no global proc named \"{str}\"");

                CallProcShared(null, proc, cArgs, arg_count, cResult);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (cArgs == null || cResult == null)
            return SetLastError("cArgs or cResult argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                DreamValue procNameVal = _dreamManager!.RefIdToValue((int)name);
                if (!procNameVal.TryGetValueAsString(out var procName))
                    return SetLastError("name argument was not a ref to a string");
                if (!_dreamManager.TryGetGlobalProc(procName, out var proc))
                    return SetLastError($"no global proc named \"{procName}\"");

                CallProcShared(null, proc, cArgs, arg_count, cResult);
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (src == null || buflen == null)
            return SetLastError("src or buflen argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                int providedBufLen = (int)*buflen;
                DreamValue srcValue = ValueFromDreamApi(*src);
                var str = srcValue.Stringify();
                var utf8 = Encoding.UTF8.GetBytes(str);
                int length = utf8.Length;

                *buflen = (uint)length + 1;
                if (buf == null)
                    return SetLastError("buf was a null pointer");
                if (providedBufLen <= length)
                    return SetLastError($"provided buf length of {providedBufLen} was less than needed {length}");

                Marshal.Copy(utf8, 0, (nint)buf, length);
                buf[length] = 0;
            } catch (Exception e) {
                *buflen = 0;
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (corner1 == null || corner2 == null || cList == null || len == null)
            return SetLastError("corner1, corner2, cList, or len argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            List<CByondValue> list = new();
            try {
                var turfs = DreamProcNativeRoot.Block(_objectTree!, _dreamMapManager!,
                    corner1->x, corner1->y, corner1->z,
                    corner2->x, corner2->y, corner2->z);

                foreach (var turf in turfs.EnumerateValues()) {
                    list.Add(ValueToByondApi(turf));
                }
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            if (*len < list.Count) {
                *len = (uint)list.Count;
                return SetLastError($"provided buf length of {*len} was less than needed {list.Count}");
            }

            *len = (uint)list.Count;
            for (int i = 0; i < list.Count; i++) {
                cList[i] = list[i];
            }

            return 1;
        });
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
        if (src == null || result == null)
            return SetLastError("src or result argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            DreamValue srcValue = ValueFromDreamApi(*src);
            try {
                *result = ValueToByondApi(DreamProcNativeRoot._length(srcValue, true));
                return 1;
            } catch (Exception e) {
                return SetLastError(e.Message);
            }
        });
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

        return RunOnMainThread<byte>(() => {
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
        });
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
        if (cType == null || cArgs == null || cResult == null)
            return SetLastError("cType, cArgs, or cResult argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                var typeVal = ValueFromDreamApi(*cType);
                if (!typeVal.TryGetValueAsType(out var treeEntry)) {
                    if (typeVal.TryGetValueAsString(out var pathString)) {
                        if (!_objectTree!.TryGetTreeEntry(pathString, out treeEntry))
                            return SetLastError($"{pathString} is not a valid type");
                    } else {
                        return SetLastError("cType argument was not a ref to a valid type");
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
                        return SetLastError($"Invalid turf loc {loc}");
                    }

                    _dreamMapManager!.SetTurf(turf, objectDef, args);
                    return 1;
                }

                var newObject = _objectTree.CreateObject(treeEntry);
                newObject.InitSpawn(args);
                *cResult = ValueToByondApi(new DreamValue(newObject));
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (cType == null || cArglist == null || cResult == null)
            return SetLastError("cType, cArglist, or cResult argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            try {
                var typeVal = ValueFromDreamApi(*cType);
                if (!typeVal.TryGetValueAsType(out var treeEntry)) {
                    if (typeVal.TryGetValueAsString(out var pathString)) {
                        if (!_objectTree!.TryGetTreeEntry(pathString, out treeEntry))
                            return SetLastError($"{pathString} is not a valid type");
                    } else {
                        return SetLastError("cType argument was not a ref to a valid type");
                    }
                }

                var objectDef = treeEntry.ObjectDefinition;

                var arglistVal = ValueFromDreamApi(*cArglist);
                if (!arglistVal.TryGetValueAsIDreamList(out var arglist))
                    return SetLastError("cArglist argument was not a list");

                // Copy the arglist's values to a new array to ensure no shenanigans
                var argValues = arglist.CopyToArray();
                var args = new DreamProcArguments(argValues);

                // TODO: This is code duplicated with DMOpcodeHandlers.CreateObject()
                if (objectDef.IsSubtypeOf(_objectTree!.Turf)) {
                    // Turfs are special. They're never created outside of map initialization
                    // So instead this will replace an existing turf's type and return that same turf
                    DreamValue loc = args.GetArgument(0);
                    if (!loc.TryGetValueAsDreamObject<DreamObjectTurf>(out var turf)) {
                        return SetLastError($"Invalid turf loc {loc}");
                    }

                    _dreamMapManager!.SetTurf(turf, objectDef, args);
                    return 1;
                }

                var newObject = _objectTree.CreateObject(treeEntry);
                newObject.InitSpawn(args);
                *cResult = ValueToByondApi(new DreamValue(newObject));
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
    }

    /** byondapi.h comment:
     * Equivalent to calling refcount(value)
     * Blocks if not on the main thread.
     * @param src The object to refcount
     * @param result Pointer to accept result
     * @return True on success
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte Byond_Refcount(CByondValue* src, uint* result) {
        if (src == null || result == null)
            return SetLastError("src or result argument was a null pointer");

        return RunOnMainThread<byte>(() => {
            // TODO
            // woah that's a lot of refs
            // i wonder if it's true??
            *result = 100;
            // (it's not)

            return 1;
        });
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
        if (src == null || xyz == null)
            return SetLastError("src or xyz argument was a null pointer");

        *xyz = new CByondXYZ();

        return RunOnMainThread<byte>(() => {
            try {
                var srcVal = ValueFromDreamApi(*src);
                if (!srcVal.TryGetValueAsDreamObject<DreamObjectAtom>(out var srcObj))
                    return SetLastError("src argument was not an atom");

                var (x, y, z) = _atomManager!.GetAtomPosition(srcObj);
                xyz->x = (short)x;
                xyz->y = (short)y;
                xyz->z = (short)z;
            } catch (Exception e) {
                return SetLastError(e.Message);
            }

            return 1;
        });
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
        if (src == null)
            return SetLastError("src argument was a null pointer");

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
        if (src == null)
            return SetLastError("src argument was a null pointer");

        throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Increase the persistent reference count of an object used in Byondapi
     * Reminder: Calls only create temporary references when made on the main thread. On other threads, the references are already persistent.
     * Blocks if not on the main thread.
     * @param src The object to incref
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_IncRef(CByondValue* src) {
        //if (src == null) return;
        //throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Mark a persistent reference as no longer in use by Byondapi
     * This is IMPORTANT to call when you make Byondapi calls on another thread, since all the references they create are persistent.
     * This cannot be used for temporary references. See ByondValue_DecTempRef() for those.
     * Blocks if not on the main thread.
     * @param src The object to decref
     */
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ByondValue_DecRef(CByondValue* src) {
        //if (src == null) return;
        //throw new NotImplementedException();
    }

    /** byondapi.h comment:
     * Mark a temporary reference as no longer in use by Byondapi
     * Temporary references will be deleted automatically at the end of a tick, so this only gets rid of the reference a little faster.
     * Only works on the main thread. Does nothing on other threads.
     * @param src The object to decref
     */
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
        if (src == null) return SetLastError("src argument was a null pointer");
        if (src->type == ByondValueType.Null) {
            return 1;
        }

        return RunOnMainThread<byte>(() => {
            var srcValue = _dreamManager!.RefIdToValue((int)src->data.@ref);

            if (srcValue == DreamValue.Null) {
                src->type = 0;
                src->data.@ref = 0;
                return 0;
            }

            return 1;
        });
    }
}
