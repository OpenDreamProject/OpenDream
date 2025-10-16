/proc/RunTest()
	var/list/L = list()

	L.Add("a")
	ASSERT(L[1] == "a")
	ASSERT(L.len == 1)

	L["a"] = 123
	ASSERT(length(L) == 1)
	ASSERT(L[1] == "a")
	ASSERT(L["a"] == 123)

	L.Add("a")
	L["a"] = 321
	ASSERT(L.len == 2)
	ASSERT(L[2] == "a")
	ASSERT(L["a"] == 321)