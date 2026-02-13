/proc/RunTest()
	var/list/A = list("thing")
	ASSERT(A[1] == "thing")
	
	A["thing"] = 6
	ASSERT(A["thing"] == 6)

	var/list/L = list()
	for(var/i in 1 to 5)
		L.Add("[i]")
		L["[i]"] = "item [i]"
	ASSERT(length(L) == 5)
	ASSERT(L["3"] == "item 3")
