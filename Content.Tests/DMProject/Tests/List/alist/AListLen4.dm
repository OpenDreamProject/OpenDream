
/proc/RunTest()
	var/alist/A = alist(1 = "a", 2 = "b")
	A.len = "e" // becomes 0
	ASSERT(length(A) == 0)
