// COMPILE ERROR
#pragma PointlessParentCall error

/datum/proc/foo()
	..()
	return

/proc/RunTest()
	return
