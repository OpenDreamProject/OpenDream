/proc/RunTest()
	var/list/A = list(1,2,3,4,5)

	ASSERT(A.len == 5)

	A.Cut()

	ASSERT(A.len == 0)
