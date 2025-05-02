var/const/lib = "../../../byondapitest.dll"

/obj/test_obj
	name = "test"

/proc/RunTest()
	var/obj/test_obj/test = new()
	var/result = call_ext(lib, "byond:byondapitest_testref_valid")(test)
	ASSERT(result == 0)

	del test
	result = call_ext(lib, "byond:byondapitest_testref_invalid")()
	ASSERT(result == 0)

	result = call_ext(lib, "byond:byondapitest_testref_null")()
	ASSERT(result == 0)