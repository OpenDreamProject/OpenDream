
C()
	return 1

/proc/C()
	return 2

/proc/RunTest()
	ASSERT(global.C() == 2)
