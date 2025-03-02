#ifndef BYONDAPI_H
#define BYONDAPI_H
#include <stddef.h>

/*
	BYOND public API version 515.1621

	Because for some reason nobody can get their ducks in a row, all of the
	exported functions from byondcore.dll/libbyond.so are limited to C
	conventions only. A header and source file for C++ wrappers is available
	for inclusion in your projects.
 */

#if defined(WIN32) || defined(WIN64)
#define IS_WINDOWS
#else
#define IS_LINUX
#endif


// See https://github.com/cpredef/predef/blob/master/Architectures.md
#if defined(i386) || defined(__i386) || defined(__i386__) || defined(_M_IX86) || defined(_X86_) || defined(__X86__)
#define _X86
#define _X86ORX64
#define DM_32BIT
#elif defined(__amd64__) || defined(__amd64) || defined(__x86_64__) || defined(__x86_64) || defined(_M_AMD64) || defined(_M_X64) || defined(_WIN64) || defined(WIN64)
#define _X64
#define _X86ORX64
#define DM_64BIT
#elif defined(__arm__) || defined(_M_ARM)
#define _ARM
#if defined(__LP64__) || defined(_LP64)
#define DM_64BIT
#else
#define DM_32BIT
#endif
#endif



/*types*/
typedef unsigned char  u1c;
typedef signed   char  s1c;
typedef unsigned short u2c;
typedef signed   short s2c;
#ifdef DM_64BIT
typedef unsigned int   u4c;
typedef signed   int   s4c;
#else
typedef unsigned long  u4c;
typedef signed   long  s4c;
#endif

#if defined(_MSC_VER) || defined(__BORLANDC__)
  typedef __int64 s8c;
  typedef unsigned __int64 u8c;
#else
  typedef long long int s8c;
  typedef unsigned long long int u8c;
#endif

union u4cOrPointer {
	u4c num;
	void *ptr;
};

#ifdef __GNUC__
#define GCC_VERSION (__GNUC__ * 10000 + __GNUC_MINOR__ * 100 + __GNUC_PATCHLEVEL__)
#else
#define GCC_VERSION 0
#endif


// determine if move-constructor and move-assignment are supported
#if defined(_MSC_VER) && _MSC_VER >= 1800
#define _SUPPORT_MOVES
#elif defined(__GNUC__) && GCC_VERSION >= 50000
#define _SUPPORT_MOVES
#endif


#define u1cMASK ((u1c)0xff)
#define u2cMASK ((u2c)0xffff)
#define u3cMASK ((u4c)0xffffffL)
#define u4cMASK ((u4c)0xffffffffL)

#define u1cMAX u1cMASK
#define u2cMAX u2cMASK
#define u3cMAX u3cMASK
#define u4cMAX u4cMASK

#define s1cMAX 0x7f
#define s1cMIN (-0x7f)
#define s2cMAX 0x7fff
#define s2cMIN (-0x7fff)
#define s4cMAX 0x7fffffffL
#define s4cMIN (-0x7fffffffL)

#define NONE u2cMAX
#define NOCH u1cMAX

#ifdef WIN32
#define THREAD_VAR __declspec(thread)
#else
#define THREAD_VAR __thread
#endif

/* dll export stuff */
#ifdef WIN32
#define DUNGPUB    __declspec(dllimport)
#define BYOND_EXPORT __declspec(dllexport)	// for functions in user-defined DLLs for use with call_ext()
#else // unix/g++, combine with -fvisibility=hidden to hide non-exported symbols
#define DUNGPUB
#define BYOND_EXPORT __attribute__ ((visibility("default")))	// for functions in user-defined .so libraries for use with call_ext()
#endif


// ByondValue

/*
	Many of the routines in this library return a bool value. If true, the
	operation succeeded. If false, it failed and calling Byond_LastError() will
	return an error string.

	The C++ wrappers change this true/false behavior to raise exceptions instead.
	This would be preferable across the board but throwing exceptions across
	module boundaries is bad juju.
 */

extern "C" {

/**
 * Gets the last error from a failed call
 * The result is a static string that does not need to be freed.
 * @return Error message
 */
DUNGPUB char const *Byond_LastError();

/**
 * Gets the current BYOND version
 * @param version Pointer to the major version number
 * @param build Pointer to the build number
 */
DUNGPUB void Byond_GetVersion(u4c *version, u4c *build);

/**
 * Gets the DMB version
 * @return Version number the .dmb was built with
 */
DUNGPUB u4c Byond_GetDMBVersion();

typedef u1c ByondValueType;
union ByondValueData {
	u4c ref;	//!< 4-byte reference ID
	float num;	//!< floating-point number
};

struct CByondValue {
	ByondValueType type;	//!< 1-byte intrinsic data type
	u1c junk1, junk2, junk3;	//!< padding
	ByondValueData data;	//!< 4-byte reference ID or floating point number
};

/**
 * Fills a CByondValue struct with a null value.
 * @param v Pointer to CByondValue
 */
DUNGPUB void ByondValue_Clear(CByondValue *v);

/**
 * Reads CByondVale's 1-byte data type
 * @param v Pointer to CByondValue
 * @return Type of value
 */
DUNGPUB ByondValueType ByondValue_Type(CByondValue const *v);

/**
 * @param v Pointer to CByondValue
 * @return True if value is null
 */
DUNGPUB bool ByondValue_IsNull(CByondValue const *v);
/**
 * @param v Pointer to CByondValue
 * @return True if value is a numeric type
 */
DUNGPUB bool ByondValue_IsNum(CByondValue const *v);
/**
 * @param v Pointer to CByondValue
 * @return True if value is a string
 */
DUNGPUB bool ByondValue_IsStr(CByondValue const *v);
/**
 * @param v Pointer to CByondValue
 * @return True if value is a list (any list type, not just user-defined)
 */
DUNGPUB bool ByondValue_IsList(CByondValue const *v);

/**
 * Determines if a value is logically true or false
 *
 * @param v Pointer to CByondValue
 * @return Truthiness of value
 */
DUNGPUB bool ByondValue_IsTrue(CByondValue const *v);

/**
 * @param v Pointer to CByondValue
 * @return Floating point number for v, or 0 if not numeric
 */
DUNGPUB float ByondValue_GetNum(CByondValue const *v);
/**
 * @param v Pointer to CByondValue
 * @return Reference ID if value is a reference type, or 0 otherwise
 */
DUNGPUB u4c ByondValue_GetRef(CByondValue const *v);

/**
 * Fills a CByondValue struct with a floating point number.
 * @param v Pointer to CByondValue
 * @param f Floating point number
 */
DUNGPUB void ByondValue_SetNum(CByondValue *v, float f);
/**
 * Creates a string and sets CByondValue to a temporary reference to that string.
 * Blocks if not on the main thread. If string creation fails, the struct is set to null.
 * @param v Pointer to CByondValue
 * @param str Null-terminated UTF-8 string
 * @see Byond_AddGetStrId()
 */
DUNGPUB void ByondValue_SetStr(CByondValue *v, char const *str);
/**
 * Fills a CByondValue struct with a reference (object) type. Does not validate.
 * @param v Pointer to CByondValue
 * @param type 1-byte teference type
 * @param ref 4-byte reference ID; for most types, an ID of NONE is invalid
 * @see Byond_TestRef()
 */
DUNGPUB void ByondValue_SetRef(CByondValue *v, ByondValueType type, u4c ref);

/**
 * Compares two values for equality
 * @param a Pointer to CByondValue
 * @param b Pointer to CByondValue
 * @return True if values are equal
 */
DUNGPUB bool ByondValue_Equals(CByondValue const *a, CByondValue const *b);

// Other useful structs

struct CByondXYZ {
	s2c x, y, z;	//!< signed 2-byte integer coordinates
	s2c junk;		//!< padding
};

/*
	In the following functions, anything that fills a result value (e.g.,
	ReadVar, CallProc) will create a temporary reference to the value. So if
	the result is an object or list or such, it will remain valid until the
	end of the current tick unless something explicitly deletes it. You can
	also let go of the temporary reference early by calling Byond_DecRef().

	If the validity of a reference is ever in doubt, call Byond_TestRef().

	Thread safety:

	Anything called outside of the main	thread will block, unless otherwise
	noted.
 */

typedef CByondValue (*ByondCallback)(void *);
/**
 * Runs a function as a callback on the main thread (or right away if already there)
 * Blocking is optional. If already on the main thread, the block parameter is meaningless.
 * @param callback Function pointer to CByondValue function(void*)
 * @param data Void pointer (argument to function)
 * @param block True if this call should block while waiting for the callback to finish; false if not
 * @return CByondValue returned by the function (if it blocked; null if not)
 */
DUNGPUB CByondValue Byond_ThreadSync(ByondCallback callback, void *data, bool block=false);

/**
 * Returns a reference to an existing string ID, but does not create a new string ID.
 * Blocks if not on the main thread.
 * @param str Null-terminated string
 * @return ID of string; NONE if string does not exist
 */
DUNGPUB u4c Byond_GetStrId(char const *str);	// does not add a string to the tree if not found; returns NONE if no string match
/**
 * Returns a reference to an existing string ID or creates a new string ID with a temporary reference.
 * Blocks if not on the main thread.
 * @param str Null-terminated string
 * @return ID of string; NONE if string creation failed
 */
DUNGPUB u4c Byond_AddGetStrId(char const *str);	// adds a string to the tree if not found

/**
 * Reads an object variable by name.
 * Blocks if not on the main thread.
 * @param loc Object that owns the var
 * @param varname Var name as null-terminated string
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_ReadVar(CByondValue const *loc, char const *varname, CByondValue *result);
/**
 * Reads an object variable by the string ID of its var name.
 * ID can be cached ahead of time for performance.
 * Blocks if not on the main thread.
 * @param loc Object that owns the var
 * @param varname Var name as string ID
 * @param result Pointer to accept result
 * @return True on success
 * @see Byond_GetStrId()
 */
DUNGPUB bool Byond_ReadVarByStrId(CByondValue const *loc, u4c varname, CByondValue *result);
/**
 * Writes an object variable by name.
 * Blocks if not on the main thread.
 * @param loc Object that owns the var
 * @param varname Var name as null-terminated string
 * @param val New value
 * @return True on success
 */
DUNGPUB bool Byond_WriteVar(CByondValue const *loc, char const *varname, CByondValue const *val);
/**
 * Writes an object variable by the string ID of its var name.
 * ID can be cached ahead of time for performance.
 * Blocks if not on the main thread.
 * @param loc Object that owns the var
 * @param varname Var name as string ID
 * @param val New value
 * @return True on success
 */
DUNGPUB bool Byond_WriteVarByStrId(CByondValue const *loc, u4c varname, CByondValue const *val);

/**
 * Creates an empty list with a temporary reference. Equivalent to list().
 * Blocks if not on the main thread.
 * @param result Result
 * @return True on success
 */
DUNGPUB bool Byond_CreateList(CByondValue *result);

/**
 * Reads items from a list.
 * Blocks if not on the main thread.
 * @param loc The list to read
 * @param list CByondValue array, allocated by caller (can be null if querying length)
 * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
 * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
 */
DUNGPUB bool Byond_ReadList(CByondValue const *loc, CByondValue *list, size_t *len);

/**
 * Writes items to a list, in place of old contents.
 * Blocks if not on the main thread.
 * @param loc The list to fill
 * @param list CByondValue array of items to write
 * @param len Number of items to write
 * @return True on success
 */
DUNGPUB bool Byond_WriteList(CByondValue const *loc, CByondValue const *list, size_t len);

/**
 * Reads items as key,value pairs from an associative list, storing them sequentially as key1, value1, key2, value2, etc.
 * Blocks if not on the main thread.
 * @param loc The list to read
 * @param list CByondValue array, allocated by caller (can be null if querying length)
 * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
 * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
 */
DUNGPUB bool Byond_ReadListAssoc(CByondValue const *loc, CByondValue *list, size_t *len);

/**
 * Reads an item from a list.
 * Blocks if not on the main thread.
 * @param loc The list
 * @param idx The index in the list (may be a number, or a non-number if using associative lists)
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_ReadListIndex(CByondValue const *loc, CByondValue const *idx, CByondValue *result);
/**
 * Writes an item to a list.
 * Blocks if not on the main thread.
 * @param loc The list
 * @param idx The index in the list (may be a number, or a non-number if using associative lists)
 * @param val New value
 * @return True on success
 */
DUNGPUB bool Byond_WriteListIndex(CByondValue const *loc, CByondValue const *idx, CByondValue const *val);

/**
 * Reads from a BYOND pointer
 * Blocks if not on the main thread.
 * @param ptr The BYOND pointer
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_ReadPointer(CByondValue const *ptr, CByondValue *result);
/**
 * Writes to a BYOND pointer
 * Blocks if not on the main thread.
 * @param ptr The BYOND pointer
 * @param val New value
 * @return True on success
 */
DUNGPUB bool Byond_WritePointer(CByondValue const *ptr, CByondValue const *val);

/*
	Proc calls:

	arg is an array of arguments; can be null arg_count is 0.

	The call is implicitly a waitfor=0 call; if the callee sleeps it will return
	immediately and finish later.
 */

/**
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
DUNGPUB bool Byond_CallProc(CByondValue const *src, char const *name, CByondValue const *arg, size_t arg_count, CByondValue *result);
/**
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
DUNGPUB bool Byond_CallProcByStrId(CByondValue const *src, u4c name, CByondValue const *arg, size_t arg_count, CByondValue *result);

/**
 * Calls a global proc by name.
 * The proc call is treated as waitfor=0 and will return immediately on sleep.
 * Blocks if not on the main thread.
 * @param name Proc name as null-terminated string
 * @param arg Array of arguments
 * @param arg_count  Number of arguments
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_CallGlobalProc(char const *name, CByondValue const *arg, size_t arg_count, CByondValue *result);	// result MUST be initialized first!
/**
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
DUNGPUB bool Byond_CallGlobalProcByStrId(u4c name, CByondValue const *arg, size_t arg_count, CByondValue *result);	// result MUST be initialized first!

/**
 * Uses BYOND's internals to represent a value as text
 * Blocks if not on the main thread.
 * @param src The value to convert to text
 * @param buf char array, allocated by caller (can be null if querying length)
 * @param buflen Pointer to length of array in bytes; receives the string length (including trailing null) on success, or required length of array if not big enough
 * @return True on success; false with *buflen=0 for failure; false with *buflen=required size if array is not big enough
 */
DUNGPUB bool Byond_ToString(CByondValue const *src, char *buf, size_t *buf_len);

// Other builtins

/**
 * Equivalent to calling block(x1,y1,z1, x2,y2,z2).
 * Blocks if not on the main thread.
 * @param corner1 One corner of the block
 * @param corner2 Another corner of the block
 * @param list CByondValue array, allocated by caller (can be null if querying length)
 * @param len Pointer to length of array (in items); receives the number of items read on success, or required length of array if not big enough
 * @return True on success; false with *len=0 for failure; false with *len=required size if array is not big enough
 */
DUNGPUB bool Byond_Block(CByondXYZ const *corner1, CByondXYZ const *corner2, CByondValue *list, size_t *len);

/**
 * Equivalent to calling length(value).
 * Blocks if not on the main thread.
 * @param src The value
 * @param result Pointer to accept result as a CByondValue (intended for future possible override of length)
 * @return True on success
 */
DUNGPUB bool Byond_Length(CByondValue const *src, CByondValue *result);

/**
 * Equivalent to calling locate(type), or locate(type) in list.
 * Blocks if not on the main thread.
 * @param type The type to locate
 * @param list The list to locate in; can be a null pointer instead of a CByondValue to locate(type) without a list
 * @param result Pointer to accept result; can be null if nothing is found
 * @return True on success (including if nothing is found); false on error
 */
DUNGPUB bool Byond_LocateIn(CByondValue const *type, CByondValue const *list, CByondValue *result);

/**
 * Equivalent to calling locate(x,y,z)
 * Blocks if not on the main thread.
 * Result is null if coords are invalid.
 * @param xyz The x,y,z coords
 * @param result Pointer to accept result
 * @return True (always)
 */
DUNGPUB bool Byond_LocateXYZ(CByondXYZ const *xyz, CByondValue *result);

/**
 * Equivalent to calling new type(...)
 * Blocks if not on the main thread.
 * @param type The type to create (type path or string)
 * @param arg Array of arguments
 * @param arg_count Number of arguments
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_New(CByondValue const *type, CByondValue const *arg, size_t arg_count, CByondValue *result);

/**
 * Equivalent to calling new type(arglist)
 * Blocks if not on the main thread.
 * @param type The type to create (type path or string)
 * @param arglist Arguments, as a reference to an arglist
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_NewArglist(CByondValue const *type, CByondValue const *arglist, CByondValue *result);	// result MUST be initialized first!

/**
 * Equivalent to calling refcount(value)
 * Blocks if not on the main thread.
 * @param src The object to refcount
 * @param result Pointer to accept result
 * @return True on success
 */
DUNGPUB bool Byond_Refcount(CByondValue const *src, size_t *result);	// result MUST be initialized first!

/**
 * Get x,y,z coords of an atom
 * Blocks if not on the main thread.
 * @param src The object to read
 * @param xyz Pointer to accept CByondXYZ result
 * @return True on success
 */
DUNGPUB bool Byond_XYZ(CByondValue const *src, CByondXYZ *xyz);	// still returns true if the atom is off-map, but xyz will be 0,0,0

/*
	Generally you don't want to mess with inc/decref calls, except that for
	temporary references you can use Byond_DecRef() to let go of the temporary
	reference.

	Call ByondValue_IncRef() to create a permanent reference to an object
	within Byondapi. It will exist until ByondValue_DecRef() removes it or the
	object is hard-deleted.

	ByondValue_DecRef() will remove a permanent reference created by Byondapi.
	If there is no permanent reference, it will remove any temporary
	reference that was set to expire when the tick ends. If Byondapi has no
	references to the object, the call will be ignored.

	These only apply to ref types, not null/num/string. Any runtime errors
	caused by decref (if the object is deleted or another object ends up
	getting deleted as an indirect result) are ignored.
 */

/**
 * Mark a reference as in use by Byondapi
 * This should be done for any temporary references returned from other calls, if they need to be saved awhile.
 * Blocks if not on the main thread.
 * @param src The object to incref
 */
DUNGPUB void ByondValue_IncRef(CByondValue const *src);

/**
 * Mark a reference as no longer in use by Byondapi
 * This can be used for temporary references to let them go immediately.
 * Blocks if not on the main thread.
 * @param src The object to decref
 */
DUNGPUB void ByondValue_DecRef(CByondValue const *src);

/**
 * Test if a reference-type CByondValue is valid
 * Blocks if not on the main thread.
 * @param src Pointer to the reference to test; will be filled with null if the reference is invalid
 * @return True if ref is valid; false if not
 */
// Returns true if the ref is valid.
// Returns false if the ref was not valid and had to be changed to null.
// This only applies to ref types, not null/num/string which are always valid.
DUNGPUB bool Byond_TestRef(CByondValue *src);

};	// extern "C"



#endif	// BYONDAPI_H
