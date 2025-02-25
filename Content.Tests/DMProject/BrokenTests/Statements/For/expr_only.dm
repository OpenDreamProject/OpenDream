
/proc/test1()
	var/x = 0
	var/list/forvals = list()
	for(x += 1)
		forvals += x
		if (x > 10)
			break
	ASSERT(x == 0)
	ASSERT(forvals.len == 0)

/proc/test2()
	var/x = 1
	var/list/forvals = list()
	for(x += 1)
		forvals += x
		if (x > 10)
			break
	ASSERT(x == 1)
	ASSERT(forvals.len == 0)

/proc/RunTest()
	test1()
	test2()
