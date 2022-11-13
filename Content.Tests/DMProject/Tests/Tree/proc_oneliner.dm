
//# issue 24

/proc/fn() return 5

/proc/RunTest()
	ASSERT(fn() == 5)
