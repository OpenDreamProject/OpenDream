var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/list/l = list("first",2)
	var/result = call_ext(lib, "byond:byondapitest_readlistindex_normal_list")(l)
	ASSERT(result == 0)
	l = list(first="first",second=2)
	result = call_ext(lib, "byond:byondapitest_readlistindex_assoc_list")(l,"first","second")
	ASSERT(result == 0)