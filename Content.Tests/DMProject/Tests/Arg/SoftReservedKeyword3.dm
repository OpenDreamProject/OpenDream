//COMPILE ERROR OD2000
// NOBYOND

#pragma SoftReservedKeyword error

/proc/RunTest()
	var/usr = 123
	world.log << "fail"
	