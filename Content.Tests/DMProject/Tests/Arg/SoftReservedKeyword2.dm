//COMPILE ERROR OD2000
// NOBYOND

#pragma SoftReservedKeyword error

/proc/test(args)
	return 1

/proc/RunTest()
	world.log << test()
	