
//# issue 564

proc
	Fn1()
		return 1
	Fn2()
		return 2

/proc/RunTest()
	ASSERT(Fn1() == 1)
	ASSERT(Fn2() == 2)
