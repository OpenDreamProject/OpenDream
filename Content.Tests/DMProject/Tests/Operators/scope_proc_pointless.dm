// COMPILE ERROR
#pragma PointlessScopeOperator error

/datum/proc/foo()
	set desc = "abc"
	return

/proc/RunTest()
	var/desc = (/datum/proc/foo::desc)
