
//# issue 484

/proc/RunTest()
	var/list/l1 = list(1,2,3)
	var/list/l2 = null
	ASSERT(l1?[2] == 2)
	ASSERT(isnull(l2?[100]))
