use meowtonin::{byond, ByondResult, ByondValue};

#[macro_use]
extern crate meowtonin;

#[byond_fn]
pub fn echo_get_version(mut obj: ByondValue) -> ByondResult<f32> {
    let (version, build) = byond().get_version();

    //obj.write_var("version", byondval!(version as f32))?;
    //obj.write_var("build", byondval!(build as f32))?;

    Ok(version as f32)
}
