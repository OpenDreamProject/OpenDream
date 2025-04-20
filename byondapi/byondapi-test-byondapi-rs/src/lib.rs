use byondapi::global_call::call_global;
use byondapi::prelude::*;

#[byondapi::bind]
fn example() -> Result<ByondValue, ()> {
    println!(
        "BYOND ver: {:?}",
        call_global("/proc/global_call_for_byondapi", &[]).unwrap()
    );
    Ok(ByondValue::null())
}
