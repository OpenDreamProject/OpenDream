var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/alist/l = list(one="first_item", "2"=2)
	var/result = call_ext(lib, "byond:byondapitest_readlistassoc_sufficient_size")(l)
	ASSERT(result == 0)
	var/result = call_ext(lib, "byond:byondapitest_readlistassoc_insufficient_size")(l)
	ASSERT(result == 0)
	var/result = call_ext(lib, "byond:byondapitest_readlistassoc_invalid_list")()
	ASSERT(result == 0)