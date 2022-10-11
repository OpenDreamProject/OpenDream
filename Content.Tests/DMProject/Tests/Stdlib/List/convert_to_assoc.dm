
//# issue 92

/proc/RunTest()
	var/list/L = list(1, 2, 3)
	L["to_assoc"] = 12
	ASSERT(L["to_assoc"] == 12)
	ASSERT(L[3] == 3)
