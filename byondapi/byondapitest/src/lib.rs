use meowtonin::{ByondResult, ByondValue, byond_version, FromByond, ToByond};
use meowtonin::sys::{
    NONE, CByondValue, Byond_GetVersion, Byond_AddGetStrId, Byond_GetStrId,
   Byond_ReadVarByStrId, Byond_WriteVar, Byond_WriteVarByStrId, Byond_ReadListAssoc
};
//use meowtonin::strid::{lookup_string_id};
use std::ffi::CString;
use std::os::raw::c_char;

#[macro_use]
extern crate meowtonin;

// TODO
#[byond_fn]
pub fn byondapitest_lasterror() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_getversion(mut obj: ByondValue) -> ByondResult<f32> {
    let mut version:u32 = 0;
    let mut build:u32 = 0;
    unsafe {
        Byond_GetVersion(&mut version as *mut u32, &mut build as *mut u32);
    }
    obj.write_var("version", byondval!(version as f32))?;
    obj.write_var("build", byondval!(build as f32))?;

    Ok(version as f32)
}

// TODO
#[byond_fn]
pub fn byondapitest_getdmbversion() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_clear() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_type() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_isnull() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_isnum() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_isstr() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_islist() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_istrue() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_getnum() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_getref() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_setnum() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_setstr() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_setref() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_equals() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_threadsync() -> ByondResult<i32> {
    Ok(0)
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_getstrid_nonexistent_id() -> ByondResult<i32> {
    let c_str = CString::new("this string does not exist").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        let result = Byond_GetStrId(c_pchar);
        match result as u16 {
            NONE => Ok(0),
            _ => Ok(1)
        }
    }
}

#[byond_fn]
pub fn byondapitest_getstrid_existent_id() -> ByondResult<u32> {
    let c_str = CString::new("this string exists").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        Ok(Byond_GetStrId(c_pchar))
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_addgetstrid_nonexistent_id() -> ByondResult<i32> {
    let c_str = CString::new("this string does not exist").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        let result = Byond_AddGetStrId(c_pchar);
        match result as u16 {
            NONE => Ok(1),
            _ => Ok(0)
        }
    }
}

#[byond_fn]
pub fn byondapitest_addgetstrid_existent_id() -> ByondResult<u32> {
    let c_str = CString::new("this string exists").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;
    unsafe {
        Ok(Byond_AddGetStrId(c_pchar))
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readvar_existent_var(loc: ByondValue) -> ByondResult<i32> {
    let key:String = "content".to_string();
    match loc.read_var::<String, String>(key) {
        Ok(x) => match x.as_str() {
            "success" => Ok(0),
            _ => Ok(1)
        },
        _ => Ok(1)
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readvar_nonexistent_var(loc: ByondValue) -> ByondResult<i32> {
    let key:String = "nonexistent_content".to_string();
    match loc.read_var::<String, String>(key) {
        Ok(_) => Ok(1),
        _ => Ok(0)
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readvarbystrid_existent_var(loc: ByondValue, key: ByondValue) -> ByondResult<i32> {
    let c_key: CByondValue = key.into_inner();
    let mut c_result: CByondValue = ByondValue::null().into_inner();

    unsafe {
        let ref_id = c_key.data.ref_;

        let is_success = Byond_ReadVarByStrId(
            &loc.into_inner(),
            ref_id,
            &mut c_result
            );

        match is_success {
            true => {
                let result = ByondValue::from(c_result);
                match result.get_string()?.as_str() {
                    "success" => Ok(0),
                    _ => Ok(1)
                }
            },
            false => Ok(1)
        }
    }
}

// 0 = succeed, 1 = fail
// This throws a runtime exception in byond, but readvar_nonexistent_var doesn't...
#[byond_fn]
pub fn byondapitest_readvarbystrid_nonexistent_var(loc: ByondValue, key: ByondValue) -> ByondResult<i32> {
    let c_key: CByondValue = key.into_inner();
    let mut c_result: CByondValue = ByondValue::null().into_inner();

    unsafe {
        let ref_id = c_key.data.ref_;

        let is_success = Byond_ReadVarByStrId(
            &loc.into_inner(),
            ref_id,
            &mut c_result
            );

        match is_success {
            true => Ok(1),
            false => Ok(0)
        }
    }
}

// 0 = succeed, 1 = fail
// needs to check outside if loc.content indeed was set to "success"
#[byond_fn]
pub fn byondapitest_writevar_existent_var(loc: ByondValue, val: ByondValue) -> ByondResult<i32> {
    let c_str = CString::new("content").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;

    unsafe {
        let is_success = Byond_WriteVar(
            &loc.into_inner(),
            c_pchar,
            &val.into_inner()
            );

        match is_success {
            true => Ok(0),
            false => Ok(1)
        }
    }
}

// 0 = succeed, 1 = fail
// This throws a runtime exception in byond, but writevar_nonexistent_var doesn't...
#[byond_fn]
pub fn byondapitest_writevar_nonexistent_var(loc: ByondValue, val: ByondValue) -> ByondResult<i32> {
    let c_str = CString::new("does_not_exist").unwrap();
    let c_pchar : *const c_char = c_str.as_ptr() as *const c_char;

    unsafe {
        let is_success = Byond_WriteVar(
            &loc.into_inner(),
            c_pchar,
            &val.into_inner()
            );

        match is_success {
            true => Ok(1),
            false => Ok(0)
        }
    }
}

// 0 = succeed, 1 = fail
// needs to check outside if loc.content indeed was set to "success"
#[byond_fn]
pub fn byondapitest_writevarbystrid_existent_var(loc: ByondValue, key: ByondValue, val: ByondValue) -> ByondResult<i32> {
    unsafe {
        let ref_id = key.into_inner().data.ref_;

        let is_success = Byond_WriteVarByStrId(
            &loc.into_inner(),
            ref_id,
            &val.into_inner()
            );

        match is_success {
            true => Ok(0),
            false => Ok(1)
        }
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_writevarbystrid_nonexistent_var(loc: ByondValue, key: ByondValue, val: ByondValue) -> ByondResult<i32> {
    unsafe {
        let ref_id = key.into_inner().data.ref_;

        let is_success = Byond_WriteVarByStrId(
            &loc.into_inner(),
            ref_id,
            &val.into_inner()
            );

        match is_success {
            true => Ok(1),
            false => Ok(0)
        }
    }
}

// return empty list = success
#[byond_fn]
pub fn byondapitest_createlist() -> ByondResult<ByondValue> {
    return Ok(ByondValue::new_list()?);
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlist_valid(list: ByondValue) -> ByondResult<i32> {
    let list = list.read_list()?;
    match (list[0].get_number()?, list[1].get_string()?.as_str(), list.len()) {
        (0.0,"one",2) => Ok(0),
        _ => Ok(1)
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlist_invalid(list: ByondValue) -> ByondResult<i32> {
    match list.read_list() {
        Ok(_) => Ok(1),
        _ => Ok(0)
    }
}

// 0 = succeed, 1 = fail
// needs to check outside if elements of list_send were put in list_rcv
#[byond_fn]
pub fn byondapitest_writelist(mut list_rcv: ByondValue, list_send: ByondValue) -> ByondResult<i32> {
    match list_rcv.write_list(Vec::from_byond(&list_send)?) {
        Ok(()) => Ok(0),
        _ => Ok(1)
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlistassoc_sufficient_size(l: ByondValue) -> ByondResult<i32> {
    let mut v: [CByondValue;6] = [Default::default(); 6];
    let mut c: usize = 6;
    unsafe {
        let result = Byond_ReadListAssoc(&l.into_inner(), v.as_mut_ptr(), &mut c);
        match (
            result,c,
            String::from_byond(&ByondValue(v[0]))?.as_str(),
            String::from_byond(&ByondValue(v[1]))?.as_str(),
            String::from_byond(&ByondValue(v[2]))?.as_str(),
            i32::from_byond(&ByondValue(v[3]))?
            ) {
            (true,4,"one","first_item", "2", 2i32) => return Ok(0),
            _ => Ok(1)
        }
    }
    /* works only in byond at last time of running... in OD ends up calling CallGlobalProcByStrId
     * and failing. Also just fails if alist(...) is used
    let v = l.read_assoc_list()?;
    match (
        v.len(),
        String::from_byond(&v[0][0])?.as_str(),
        String::from_byond(&v[0][1])?.as_str(),
        f32::from_byond(&v[1][0])?,
        f32::from_byond(&v[1][1])?
        ) {
        (2,"one","first_item",2f32,2f32) => Ok(0),
        _ => Ok(1)
    }*/
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlistassoc_insufficient_size(l: ByondValue) -> ByondResult<i32> {
    let mut v: [CByondValue;2] = [Default::default(); 2];
    let mut c: usize = 2;
    unsafe {
        let result = Byond_ReadListAssoc(&l.into_inner(), v.as_mut_ptr(), &mut c);
        match (result,c) {
            (false,4) => return Ok(0),
            _ => Ok(1)
        }
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlistassoc_invalid_list() -> ByondResult<i32> {
    let mut v: [CByondValue;2] = [Default::default(); 2];
    let mut c: usize = 2;
    let not_l = i32::to_byond(&1)?;
    unsafe {
        let result = Byond_ReadListAssoc(&not_l.into_inner(), v.as_mut_ptr(), &mut c);
        match (result,c) {
            (false,0) => return Ok(0),
            _ => Ok(1)
        }
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlistindex_normal_list(loc: ByondValue) -> ByondResult<i32> {
    let first: String = loc.read_list_index(&1)?;
    let second: i32 = loc.read_list_index(&2)?;
    match (first.as_str(),second) {
        ("first",2) => Ok(0),
        _ => Ok(1)
    }
}

// 0 = succeed, 1 = fail
#[byond_fn]
pub fn byondapitest_readlistindex_assoc_list(loc: ByondValue, key1: ByondValue, key2: ByondValue) -> ByondResult<i32> {
    let first: String = loc.read_list_index(&key1)?;
    let second: i32 = loc.read_list_index(&key2)?;
    match (first.as_str(),second) {
        ("first",2) => Ok(0),
        _ => Ok(1)
    }
}

// TODO
#[byond_fn]
pub fn byondapitest_writelistindex() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_readpointer() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_writepointer() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_callproc() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_callprocbystrid() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_tostring() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_block() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_length() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_locatein() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_locatexyz() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_new() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_newarglist() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_refcount() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_xyz() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_incref() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_decref() -> ByondResult<i32> {
    Ok(0)
}

// TODO
#[byond_fn]
pub fn byondapitest_testref() -> ByondResult<i32> {
    Ok(0)
}
