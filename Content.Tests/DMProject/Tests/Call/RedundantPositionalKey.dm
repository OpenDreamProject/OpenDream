// COMPILE ERROR

#pragma PointlessPositionalArgument error

/proc/test()

/proc/RunTest()
	test(1 = "a", 2 = "b", 3 = "c") // 3 unnecessary positional keys