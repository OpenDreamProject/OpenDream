/proc/RunTest()
	var/list/L = list(1, 2, 3)
	L -= 2
	ASSERT(!(2 in L))