//COMPILE ERROR OD2000
// NOBYOND
/proc/test(args)
	return 1

/proc/RunTest()
	world.log << test()
	