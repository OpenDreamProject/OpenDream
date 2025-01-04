
/proc/RunTest()
	var/list/forvals = list()
	for(var/x += 1)
		forvals += x
		if (x > 10)
			break
	ASSERT(forvals.len == 0)
