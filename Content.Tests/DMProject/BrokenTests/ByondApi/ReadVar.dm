var/const/lib = "../../../byondapitest.dll"

/obj/test
	var/content = "success"

/proc/RunTest()
	var/obj/test/foo = new()
	var/result = call_ext(lib, "byond:byondapitest_readvar_existent_var")(foo)
	ASSERT(result == 0)
	result = call_ext(lib, "byond:byondapitest_readvar_nonexistent_var")(foo)
	ASSERT(result == 0)