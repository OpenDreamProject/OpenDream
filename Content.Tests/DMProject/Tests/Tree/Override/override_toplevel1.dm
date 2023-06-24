
/proc/C()
	return 1

/var/z = 5

C()
	return 2

/proc/RunTest()
	ASSERT(global.C() == 1)
