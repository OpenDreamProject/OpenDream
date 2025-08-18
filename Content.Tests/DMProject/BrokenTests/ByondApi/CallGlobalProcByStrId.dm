var/const/lib = "../../../byondapitest.dll"

proc/proc_name(input_one, input_two)
	world.log << "one: [input_one]; two: [input_two];"
	if (input_one == 1 && input_two == "two")
		return 1
	else
		return 0

/proc/RunTest()
	var/result = call_ext(lib, "byond:byondapitest_callglobalprocbystrid")("proc_name",1,"two")
	ASSERT(result == 0)