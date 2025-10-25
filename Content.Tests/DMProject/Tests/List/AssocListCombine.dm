/proc/RunTest()
	var/list/L = list("A")
	L |= list("B" = 1)
	ASSERT(L ~= list("A", "B" = 1))
	ASSERT(L["A"] == null)
	ASSERT(L["B"] == 1)

	L = list("A")
	L |= list("A" = 1)
	ASSERT(L ~= list("A"))
	ASSERT(L["A"] == null) // If the key already exists, it won't copy the associated value
