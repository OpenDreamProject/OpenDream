var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/result = call_ext(lib, "byond:get_nonexistent_id")()
	ASSERT(result == 0)

	var/exists = "this string exists";
	result = call_ext(lib, "byond:get_existent_id")()
	result = "\[0x6" + num2text(result,6,16) + "\]"
	var/theref = ref(exists)
	ASSERT(result == ref(exists))