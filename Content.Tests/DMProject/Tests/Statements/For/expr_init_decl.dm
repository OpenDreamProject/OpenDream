
/proc/RunTest()
	var/list/forvals = list()
	for (var/a && var/b, a < (b + 10), a += 2)
		forvals += a
	ASSERT(forvals.len == 5)
	ASSERT(forvals[3] == 4) //cba to check them all
