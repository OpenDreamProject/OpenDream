/proc/RunTest()
	var/list/L = list()
	L.Cut()
	// Incase of weird proc issues
	ASSERT(L.len == 0)
