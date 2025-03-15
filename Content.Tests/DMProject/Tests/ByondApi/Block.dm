var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/list/l = new()
	call_ext(lib, "byond:byondapitest_block")(l)
	ASSERT(length(l ^ block(1,1,1,2,3,2)) == 0)