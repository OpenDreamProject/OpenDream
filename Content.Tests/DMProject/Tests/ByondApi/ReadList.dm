var/const/lib = "../../../byondapitest.dll"

/obj/test
	var/content = "success"

/proc/RunTest()
	var/list/a_list = list(0,"one")
	var/result = call_ext(lib, "byond:byondapitest_readlist_valid")(a_list)
	ASSERT(result == 0)
	var/not_a_list = 1
	result = call_ext(lib, "byond:byondapitest_readlist_invalid")(not_a_list)
	ASSERT(result == 0)