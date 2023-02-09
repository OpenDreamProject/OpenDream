
/proc/RunTest()
	var/list/L1 = typesof(null)
	ASSERT(islist(L1))
	ASSERT(L1.len == 0)

	var/list/L2 = typesof(5)
	ASSERT(islist(L2))
	ASSERT(L2.len == 0)
