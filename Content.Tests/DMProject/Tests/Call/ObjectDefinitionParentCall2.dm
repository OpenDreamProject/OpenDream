// COMPILE ERROR OD2205

#pragma PointlessParentCall error

/datum
	..()
	var/meep = "foo"

/proc/RunTest()
	var/datum/D = new
	ASSERT(D.meep == "foo")
