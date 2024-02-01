//# issue 632

/proc/RunTest()
	var/list/L = list(1,2,3,2,1)
	L.Remove(2)
	ASSERT(L[2] == 2)
	ASSERT(L[3] == 3)
	ASSERT(L[4] == 1)
	
	L = list(1,2,3,2,1)
	L.Remove(list(2))
	ASSERT(L ~= list(1,2,3,1))
