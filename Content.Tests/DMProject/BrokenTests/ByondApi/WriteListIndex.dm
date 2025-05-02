var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/list/l = list("first",2)
	call_ext(lib, "byond:byondapitest_writelistindex_normal_list")(l,"first",2)
	ASSERT(l[1]=="first")
	ASSERT(l[2]==2)

	l = list(first="first",second=2)
	call_ext(lib, "byond:byondapitest_writelistindex_assoc_list")(l,"first","first","second",2)
	ASSERT(l["first"]=="first")
	ASSERT(l["second"]==2)