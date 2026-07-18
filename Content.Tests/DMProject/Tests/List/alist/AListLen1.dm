
/proc/RunTest()
	var/alist/A = alist(1 = "a", 2 = "b")
	ASSERT(A.len == 2)
	ASSERT(A.len == length(A))