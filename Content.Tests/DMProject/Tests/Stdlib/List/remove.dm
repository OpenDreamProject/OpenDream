//# issue 632

/proc/RunTest()
	var/list/L = list(1,2,3,2,1)
	L.Remove(2)
	ASSERT(L ~= list(1,2,3,1))
	
	L = list(1,2,3,2,1)
	L.Remove(list(2))
	ASSERT(L ~= list(1,2,3,1))

	L = list(1,2,3,2,1)
	L.Remove(L)
	ASSERT(L ~= list())