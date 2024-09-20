
/proc/RunTest()
	var/list/l1 = list(1,2,3)
	ASSERT((1 in l1 in l1) == 1)
	ASSERT((1 in list(3) in list(0)) == 1)
	