var/const/byondapi_test_lib_2 = "../byondapi/target/debug/libbyondapi_test_byondapi_rs.so"

/world/New()
	..()
	world.log << "Running byondapi_test_lib_2 test!"
	var/result = call_ext(byondapi_test_lib_2, "byond:example_crash_ffi")()
	world.log << "result: [result]"

/proc/global_call_for_byondapi()
	return "REAL"
