use meowtonin::{ByondResult, ByondValue, byond_version};
use meowtonin::sys::{CByondValue,ByondValueData};

#[macro_use]
extern crate meowtonin;

/*
#[byond_fn]
pub fn echo_get_version(mut obj: ByondValue) -> ByondResult<f32> {
    let (version, build) = byond().get_version();

    //obj.write_var("version", byondval!(version as f32))?;
    //obj.write_var("build", byondval!(build as f32))?;

    Ok(version as f32)
}
*/


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


// see https://github.com/rust-lang/reference/issues/638
#[cfg_attr(all(target_os = "windows", target_env = "msvc"), link(name = "byondcore.dll"))]
#[cfg_attr(not(all(target_os = "windows", target_env = "msvc")), link(name = "byondcore"))]
extern "C" {
    fn ByondValue_SetNum(value: *mut CByondValue, newVal: f32);
    fn Byond_GetVersion(version: *mut u32, build: *mut u32);
}

/*
#[byond_fn]
pub fn arithmetic_add(a: f32, b: f32) -> ByondResult<f32> {
    Ok(a + b)
}
*/
