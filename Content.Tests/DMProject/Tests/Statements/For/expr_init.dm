
/proc/RunTest()
	var/list/forvals = list()

	var a = 1
	for (a && a, a < 2, a++)
		forvals += a

	ASSERT(a == 2)

	for (a *= 3, a < 10, a++)
		forvals += a

	ASSERT(a == 10)
	ASSERT(forvals.len == 5)
