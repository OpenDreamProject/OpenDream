/proc/RunTest()
	var/list/L1 = list(1, 2, 3)
	var/list/L2 = list(4, 5, 6)
	var/list/L3 = list(4, 5)

	L1.Add(L2)
	L1.Remove(L3)

	ASSERT(L1[1] == 1)
	ASSERT(L1[2] == 2)
	ASSERT(L1[3] == 3)
	ASSERT(L1[4] == 6)
