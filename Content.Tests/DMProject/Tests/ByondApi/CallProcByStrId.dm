var/const/lib = "../../../byondapitest.dll"

obj/testType
	proc/proc_name(input_one, input_two)
		if (input_one == 1 && input_two == "two")
			return 1
		else
			return 0

/proc/RunTest()
	var/obj/testType/x = new()
	var/result = call_ext(lib, "byond:byondapitest_callprocbystrid")(x,"proc_name",1,"two")
	ASSERT(result == 0)