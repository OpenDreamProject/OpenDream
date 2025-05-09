var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/result = call_ext(lib, "byond:byondapitest_createlist")()
	ASSERT(islist(result))
	ASSERT(length(result) == 0)