// COMPILE ERROR OD3300
#pragma RuntimeSearchOperator error
/datum/proc/foo()
	return

/proc/RunTest()
	var/datum/D = new
	D:foo()
