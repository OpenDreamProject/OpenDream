var/const/lib = "../../../byondapitest.dll"

/obj/test
	var/content = "fail"

/proc/RunTest()
	var/obj/test/foo = new()
	var/result = call_ext(lib, "byond:byondapitest_writevar_existent_var")(foo, "success")
	ASSERT(result == 0)
	ASSERT(foo.content == "success")
	result = call_ext(lib, "byond:byondapitest_writevar_nonexistent_var")(foo)
	ASSERT(result == 0)