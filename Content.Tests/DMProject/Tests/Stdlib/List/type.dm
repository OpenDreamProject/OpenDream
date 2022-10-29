
//# issue 701

/proc/RunTest()
	var/list/L = list(2)
	ASSERT(L.type == /list)
