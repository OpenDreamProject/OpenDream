#![allow(non_upper_case_globals)]
#![allow(non_camel_case_types)]
#![allow(non_snake_case)]

mod api {
    #![allow(dead_code)]
    include!(concat!(env!("OUT_DIR"), "/bindings.rs"));
}

const TYPE_NULL: u8 = 0;
const TYPE_TURF: u8 = 0x01;
const TYPE_OBJ: u8 = 0x02;
const TYPE_MOB: u8 = 0x03;
const TYPE_AREA: u8 = 0x04;
const TYPE_CLIENT: u8 = 0x05;
const TYPE_STRING: u8 = 0x06;
const TYPE_MOBTYPEPATH: u8 = 0x08;
const TYPE_OBJTYPEPATH: u8 = 0x09;
const TYPE_TURFTYPEPATH: u8 = 0x0A;
const TYPE_AREATYPEPATH: u8 = 0x0B;
const TYPE_IMAGE: u8 = 0x0D;
const TYPE_WORLD: u8 = 0x0E;
const TYPE_LIST: u8 = 0x0F;
const TYPE_DATUMTYPEPATH: u8 = 0x20;
const TYPE_DATUM: u8 = 0x21;
const TYPE_NUMBER: u8 = 0x2A;
const TYPE_POINTER: u8 = 0x3C;

use api::{u4c, ByondValueData, ByondValueType, CByondValue, CByondXYZ, ByondCallback};
use std::ffi::{c_char, c_void};
use std::sync::OnceLock;

static TRAMPOLINES: OnceLock<Trampolines> = OnceLock::new();

macro_rules! trampolines {
    ( $( fn $name:ident ( $( $param:ident: $paramType:ty ),* ) $(-> $ret:ty )? );* $(;)? ) => {
        $(
            #[no_mangle]
            unsafe extern "C" fn $name( $( $param: $paramType, )* ) $(-> $ret )? {
                // TOOD: Remove me when done w/ testing
                println!(stringify!($name));
                return (TRAMPOLINES.get().unwrap().$name)($( $param, )*);
            }
        )*

        #[repr(C)]
        #[derive(Clone, Copy)]
        struct Trampolines {
            $(
                $name: extern "C" fn( $( $param: $paramType, )* )  $(-> $ret )?,
            )*
        }
    };
}

#[no_mangle]
unsafe extern "C" fn OpenDream_Internal_Init(trampolines: *const Trampolines) {
    TRAMPOLINES.set(*trampolines).map_err(|_| ()).expect("OpenDream_Internal_Init can only be called once!");
}

#[no_mangle]
unsafe extern "C" fn ByondValue_Clear(v: *mut CByondValue) {
    *v = core::mem::zeroed();
}

#[no_mangle]
unsafe extern "C" fn ByondValue_Type(v: *const CByondValue) -> u8 {
    (*v).type_
}

//
// ByondValue type checks
//

#[no_mangle]
unsafe extern "C" fn ByondValue_IsNull(v: *const CByondValue) -> bool {
    (*v).type_ == TYPE_NULL
}

#[no_mangle]
unsafe extern "C" fn ByondValue_IsNum(v: *const CByondValue) -> bool {
    (*v).type_ == TYPE_NUMBER
}

#[no_mangle]
unsafe extern "C" fn ByondValue_IsStr(v: *const CByondValue) -> bool {
    (*v).type_ == TYPE_STRING
}

#[no_mangle]
unsafe extern "C" fn ByondValue_IsList(v: *const CByondValue) -> bool {
    (*v).type_ == TYPE_LIST
}

#[no_mangle]
unsafe extern "C" fn ByondValue_IsTrue(v: *const CByondValue) -> bool {
    unimplemented!()
}

//
// ByondValue access
//

#[no_mangle]
unsafe extern "C" fn ByondValue_GetNum(v: *const CByondValue) -> f32 {
    match (*v).type_ {
        TYPE_NUMBER => (*v).data.num,
        _ => 0.0,
    }
}

#[no_mangle]
unsafe extern "C" fn ByondValue_GetRef(v: *const CByondValue) -> api::u4c {
    match (*v).type_ {
        TYPE_TURF | TYPE_OBJ | TYPE_MOB | TYPE_AREA | TYPE_CLIENT | TYPE_STRING
        | TYPE_MOBTYPEPATH | TYPE_OBJTYPEPATH | TYPE_TURFTYPEPATH | TYPE_AREATYPEPATH
        | TYPE_IMAGE | TYPE_WORLD | TYPE_LIST | TYPE_DATUMTYPEPATH | TYPE_DATUM | TYPE_POINTER => {
            (*v).data.ref_
        }
        _ => 0,
    }
}

#[no_mangle]
unsafe extern "C" fn ByondValue_SetNum(v: *mut CByondValue, f: f32) {
    *v = CByondValue {
        type_: TYPE_NUMBER,
        junk1: 0,
        junk2: 0,
        junk3: 0,
        data: ByondValueData { num: f },
    }
}

#[no_mangle]
unsafe extern "C" fn ByondValue_SetStr(v: *mut CByondValue, str: *const c_char) {
    unimplemented!()
}

#[no_mangle]
unsafe extern "C" fn ByondValue_SetRef(
    v: *const CByondValue,
    r#type: ByondValueType,
    r#ref: u4c,
) {
    unimplemented!()
}

#[no_mangle]
unsafe extern "C" fn ByondValue_Equals(a: *const CByondValue, b: *const CByondValue) -> bool {
    unimplemented!()
}

//
// Byond functions
//

trampolines! {
    fn Byond_LastError() -> *const c_char;
    fn Byond_GetVersion(version: *mut u4c, build: *mut u4c);
    fn Byond_GetDMBVersion() -> u4c;
    fn Byond_ThreadSync(callback: ByondCallback, data: *mut c_void, block: bool) -> CByondValue;
    fn Byond_GetStrId(str: *const c_char) -> u4c;
    fn Byond_AddGetStrId(str: *const c_char) -> u4c;
    fn Byond_ReadVar(loc: *const CByondValue, varname: *const c_char, result: *mut CByondValue) -> bool;
    fn Byond_ReadVarByStrId(loc: *const CByondValue, varname: u4c, result: *mut CByondValue) -> bool;
    fn Byond_WriteVar(loc: *const CByondValue, varname: *const c_char, val: *const CByondValue) -> bool;
    fn Byond_WriteVarByStrId(loc: *const CByondValue, varname: u4c, val: *const CByondValue) -> bool;
    fn Byond_CreateList(result: *mut CByondValue) -> bool;
    fn Byond_ReadList(loc: *const CByondValue, list: *mut CByondValue, len: *mut u4c) -> bool;
    fn Byond_WriteList(loc: *const CByondValue, list: *const CByondValue, len: u4c) -> bool;
    fn Byond_ReadListAssoc(loc: *const CByondValue, list: *mut CByondValue, len: *mut u4c) -> bool;
    fn Byond_ReadListIndex(loc: *const CByondValue, idx: *const CByondValue, result: *mut CByondValue) -> bool;
    fn Byond_WriteListIndex(loc: *const CByondValue, idx: *const CByondValue, val: *const CByondValue) -> bool;
    fn Byond_ReadPointer(ptr: *const CByondValue, result: *mut CByondValue) -> bool;
    fn Byond_WritePointer(ptr: *const CByondValue, val: *const CByondValue) -> bool;
    fn Byond_CallProc(src: *const CByondValue, name: *const c_char, arg: *const CByondValue, arg_count: u4c, result: *mut CByondValue) -> bool;
    fn Byond_CallProcByStrId(src: *const CByondValue, name: u4c, arg: *const CByondValue, arg_count: u4c, result: *mut CByondValue) -> bool;
    fn Byond_CallGlobalProc(name: *const c_char, arg: *const CByondValue, arg_count: u4c, result: *mut CByondValue) -> bool;
    fn Byond_CallGlobalProcByStrId(name: u4c, arg: *const CByondValue, arg_count: u4c, result: *mut CByondValue) -> bool;
    fn Byond_ToString(src: *const CByondValue, buf: *mut c_char, buflen: *mut u4c) -> bool;

    // "Proc calls"
    fn Byond_Block(corner1: *const CByondXYZ, corner2: *const CByondXYZ, list: *mut CByondValue, len: *mut u4c) -> bool;
    fn Byond_Length(src: *const CByondValue, result: *mut CByondValue) -> bool;
    fn Byond_LocateIn(r#type: *const CByondValue, list: *const CByondValue, result: *mut CByondValue) -> bool;
    fn Byond_LocateXYZ(xyz: *const CByondXYZ, result: *mut CByondValue) -> bool;
    fn Byond_New(r#type: *const CByondValue, arg: *const CByondValue, arg_count: u4c, result: *mut CByondValue) -> bool;
    fn Byond_NewArglist(r#type: *const CByondValue, arglist: *const CByondValue, result: *mut CByondValue) -> bool;
    fn Byond_Refcount(src: *const CByondValue, result: *mut u4c) -> bool;
    fn Byond_XYZ(src: *const CByondValue, xyz: *mut CByondXYZ) -> bool;

    // Ref counting
    fn ByondValue_IncRef(src: *const CByondValue);
    fn ByondValue_DecRef(src: *const CByondValue);
    fn Byond_TestRef(src: *mut CByondValue) -> bool;
}
