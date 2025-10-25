/proc/RunTest()
	var/i = 0
	var/j = 0
	var/list/L1 = null
	var/list/L2 = list(1, 2, 3, 4)

	for (L1?[i++] in L2)
		j++

	ASSERT(i == 0)
	ASSERT(j == 4)