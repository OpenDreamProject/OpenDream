// COMPILE ERROR OD2209
#pragma PointlessScopeOperator error

/datum/proc/foo()
	set desc = "abc"
	return

/proc/RunTest()
	var/desc = (/datum/proc/foo::desc)
