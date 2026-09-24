//COMPILE ERROR OD2000
// NOBYOND

#pragma SoftReservedKeyword error

var/src = 321

/proc/RunTest()
	world.log << "fail"
	