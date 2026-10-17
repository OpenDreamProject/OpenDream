// COMPILE ERROR OD2205

#pragma PointlessParentCall error

/datum/proc/foo()
	CRASH("unreachable")

/datum/meep/foo()
	. = TRUE
..()

/proc/RunTest()
	var/datum/meep/M = new
	return M.foo()