var/const/lib = "../../../byondapitest.dll"

/proc/TestArithmetic()
	var/result = call_ext(lib, "byond:arithmetic_add")(5, 7)
	ASSERT(result == 12)

/proc/RunTest()
	TestArithmetic()