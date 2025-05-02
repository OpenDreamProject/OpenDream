var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/list/list_rcv = list(0,"one",2)
	var/list/list_send = list("zero",1)
	var/result = call_ext(lib, "byond:byondapitest_writelist")(list_rcv,list_send)
	ASSERT(result == 0)
	ASSERT(list_rcv[1] == "zero")
	ASSERT(list_rcv[2] == 1)
	ASSERT(list_rcv.len == 2)