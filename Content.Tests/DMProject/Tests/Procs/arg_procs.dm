/proc/ArgProcs1(procs, procs2, procs3 = 3)
	return procs3

/proc/RunTest()
	ASSERT(ArgProcs1(1,2) == 3)
	ASSERT(ArgProcs1(1,2,4) == 4)
	ASSERT(ArgProcs1("procs"= 4 ) == 3)
	ASSERT(ArgProcs1("procs"= 4, "procs"= 5, "procs" = 6) == 3)
	ASSERT(ArgProcs1(1, 2, 4, "procs" = 5) == 4)
