/proc/RunTest()
	var/list/L = list(1,2,3)
	var/list/N = L
	N -= 2
	ASSERT(length(N) == 2)
	ASSERT(length(L) == 2)
	