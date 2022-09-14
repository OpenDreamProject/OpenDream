
//# issue 384

/proc/RunTest()
	var/l1 = list(1,2,3)
	var/l2 = list(1,2,3,4)
	var/l3 = list(1,2)
	var/l4 = list(1,2,3,4)
	ASSERT((l1 ~= l1) == TRUE)
	ASSERT((l1 ~= l2) == FALSE)
	ASSERT((l1 ~= l3) == FALSE)
	ASSERT((l1 ~= l3) == FALSE)
	ASSERT((l1 ~! l1) == FALSE)
	ASSERT((l1 ~! l2) == TRUE)
	ASSERT((l1 ~! l3) == TRUE)
	ASSERT((l1 ~! l4) == TRUE)
