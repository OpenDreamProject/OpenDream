
//# issue 562

/proc/RunTest()
	var/list/L = list(1,2,3)
	L.len = "a string!"
	ASSERT(L.len == 0)
