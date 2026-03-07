// NOBYOND
#pragma PointlessParentCall error

// don't emit the pragma if lateral overrides exist
/datum/foo()
	return
/datum/proc/foo()
	..()
	return

/proc/RunTest()
	return
