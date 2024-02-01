
/proc/RunTest()
	var/list/L = list(1,2,3,2,1)
	L.RemoveAll(2)
	ASSERT(L ~= list(1, 3, 1))
	
	L = list(1,2,3,2,1)
	L.RemoveAll(list(2))
	ASSERT(L[2] == 3)
	ASSERT(L[3] == 1)
	ASSERT(length(L) == 3)
