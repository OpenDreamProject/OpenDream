
//# issue 670

/proc/RunTest()
	var/list/a = list(1,2,3)
	var/list/b = list(4)
	ASSERT((1 ? a : b).len == 3)
