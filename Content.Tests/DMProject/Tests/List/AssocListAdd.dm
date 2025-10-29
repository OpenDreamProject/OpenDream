/proc/RunTest()
	var/list/L1 = list("A" = 1)
	var/list/L2 = list("B" = 2)

	ASSERT( (L1 + L2)["B"] == 2 )

	L1 += L2
	ASSERT(L1["B"] == 2)
	ASSERT(L1.len == 2)

	// Lists are fun
	L1 += L2
	ASSERT(L1[3] == "B")
	ASSERT(L1.len == 3)