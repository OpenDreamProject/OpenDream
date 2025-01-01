
/proc/RunTest()
	ASSERT((5 in 4 to 10) == TRUE)
	var/x = 3 in 2 to 5
	ASSERT(x == 3)
	x = 3 in 2 to 5
	ASSERT(x == 3)
