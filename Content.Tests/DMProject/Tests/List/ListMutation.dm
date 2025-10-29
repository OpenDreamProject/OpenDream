/proc/RunTest()
	var/list/L = list(1, 2, 3)
	L[2] *= 15
	L[3] *= 0

	ASSERT(L ~= list(1, 30, 0))