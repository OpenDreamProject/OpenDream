use meowtonin::{ByondResult, ByondValue, byond_version};
use meowtonin::sys::{NONE, u4c, CByondValue,ByondValueData, Byond_GetVersion, ByondValue_SetNum, Byond_GetStrId};
//use meowtonin::strid::{lookup_string_id};
use std::ffi::CString;
use std::os::raw::c_char;

#[macro_use]
extern crate meowtonin;


#[byond_fn]
pub fn echo_get_version(mut obj: ByondValue) -> ByondResult<f32> {
    let mut version:u32 = 0;
    let mut build:u32 = 0;
    unsafe {
        Byond_GetVersion(&mut version as *mut u32, &mut build as *mut u32);
    }
    obj.write_var("version", byondval!(version as f32))?;
    obj.write_var("build", byondval!(build as f32))?;

    Ok(version as f32)
}


/*
#[no_mangle]
pub extern "C" fn echo_get_version(n: i32, v: *mut CByondValue) -> CByondValue {
    let mut version:u32 = 0;
    let mut build:u32 = 0;
    unsafe {
        Byond_GetVersion(&mut version as *mut u32, &mut build as *mut u32);
    }
    let mut val = CByondValue{type_:0,junk1:0,junk2:0,junk3:0,data:ByondValueData{num:0f32}};
    unsafe {
    //    ByondValue_SetNum(&mut val as *mut CByondValue, 100.0);
        ByondValue_SetNum(&mut val as *mut CByondValue, version as f32);
    }
    val
}
*/



#[byond_fn]
pub fn arithmetic_add(a: f32, b: f32) -> ByondResult<f32> {
    Ok(a + b)
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn get_nonexistent_id() -> ByondResult<i32> {
    let c_str = CString::new("this string does not exist").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        let x = Byond_GetStrId(c_pchar);
        match x == NONE as u32 {
            true => Ok(0),
            false => Ok(1)
        }
    }
}

#[byond_fn]
pub fn get_existent_id() -> ByondResult<u32> {
    let c_str = CString::new("this string exists").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        println!("test");
        Ok(Byond_GetStrId(c_pchar))
    }
}

