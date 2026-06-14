/proc/RunTest()
	var/list/L = list("a", "b", "c", "d")

	ASSERT(L[3] == "c")
	ASSERT(length(L) == 4)
	L.len--
	ASSERT(length(L) == 3)
	L.len -= 1
	ASSERT(length(L) == 2)
	L.len = 1
	ASSERT(length(L) == 1)
	ASSERT(L[1] == "a")