var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/result = call_ext(lib, "byond:byondapitest_tostring")("abc")
	ASSERT(result == 0)