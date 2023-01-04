/proc/RunTest()
	var/list/L[1]
	ASSERT(NullCheck(L, 1))
	L.len = 5
	ASSERT(NullCheck(L, 5))
	L.len = 3
	ASSERT(NullCheck(L, 3))


/proc/NullCheck(var/list/listA, length)
	if(listA.len != length)
		return 0
	for(var/item in listA)
		if(!isnull(item))
			return 0
	return 1
