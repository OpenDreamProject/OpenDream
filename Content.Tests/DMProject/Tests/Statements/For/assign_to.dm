
/proc/RunTest()
	var/list/forvals = list()

	for (var/a = 1 to 8 step 3)
		forvals += a
	
	ASSERT(forvals.len == 3)
	ASSERT(forvals[1] == 1)
	ASSERT(forvals[2] == 4)
	ASSERT(forvals[3] == 7)