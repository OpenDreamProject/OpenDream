/proc/RunTest()
	var/list/A = list("thing")
	ASSERT(A[1] == "thing")
	
	A["thing"] = 6
	ASSERT(A["thing"] == 6)