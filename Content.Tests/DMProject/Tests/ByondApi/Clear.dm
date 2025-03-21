var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/result = call_ext(lib, "byond:byondapitest_clear")()
	ASSERT(result == 0)