/proc/RunTest()
	var/list/L = list(1, 2, 3)
	ASSERT(!(4 in L))
	ASSERT(3 in L)