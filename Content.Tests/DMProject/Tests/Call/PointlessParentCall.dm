// COMPILE ERROR OD2205
#pragma PointlessParentCall error

/datum/proc/foo()
	..()
	return

/proc/RunTest()
	return
