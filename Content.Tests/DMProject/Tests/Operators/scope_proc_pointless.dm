// COMPILE ERROR
#pragma PointlessScopeOperator error

/datum/proc/foo()
	return

/proc/RunTest()
	var/desc = (/datum/proc/foo::desc)
