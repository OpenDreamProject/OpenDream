/proc/RunTest()
	L1 = list(1, 2)
	L1 += L1
	ASSERT(L1 ~= list(1, 2, 1, 2))

	L2 = list(a=1, b=2)
	L2 += L2
	ASSERT(L2 ~= list(a=1, b=2, "a", "b"))
