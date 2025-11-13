/proc/RunTest()
	var/list/L = list("a", "b", "c")

	ASSERT(length(L) == 3)
	ASSERT(length(L) == L.len)