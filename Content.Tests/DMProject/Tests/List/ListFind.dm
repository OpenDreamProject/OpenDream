/proc/RunTest()
	var/list/L = list("a", "b", "c")
	
	ASSERT(L.Find("b") == 2)
	
	ASSERT(L.Find("b", 1) == 2)
	ASSERT(L.Find("b", 2) == 2)
	ASSERT(L.Find("b", 3) == 0)
	
	ASSERT(L.Find("b", 1, 0) == 2)
	ASSERT(L.Find("b", 2, 0) == 2)
	ASSERT(L.Find("b", 3, 0) == 0)
	
	ASSERT(L.Find("b", 1, 1) == 0)
	ASSERT(L.Find("b", 1, 2) == 2)
	ASSERT(L.Find("b", 1, 3) == 2)
	
	ASSERT(L.Find("b", new /datum) == 2)
	ASSERT(L.Find("b", "c") == 2)
	ASSERT(L.Find("b", 1, new /datum) == 2)