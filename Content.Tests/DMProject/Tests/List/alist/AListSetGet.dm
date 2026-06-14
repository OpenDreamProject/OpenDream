/proc/RunTest()
	var/alist/A1 = alist("1" = 1)
	ASSERT(A1["1"] == 1)
	A1["1"] = 1.5
	A1["2"] = 2
	ASSERT(A1["1"] == 1.5)
	ASSERT(A1["2"] == 2)