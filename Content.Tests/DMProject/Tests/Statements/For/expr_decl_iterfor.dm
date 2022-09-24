
/proc/RunTest()
	var/list/L = list(1,2,3,4)
	var/list/forvals = list()
	for(var/x in L)
		forvals += x
	ASSERT(forvals.len == 4)
	ASSERT(forvals[3] == 3) // cba to check them all
