/proc/RunTest()
	var/list/L = list()
	L[new()] = new()

	ASSERT(islist(L))
	ASSERT(islist(L[1]))