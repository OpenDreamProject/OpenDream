// COMPILE ERROR OD2701
#pragma InvalidReturnType error

/datum/foo

/datum/proc/meep() as /atom
	return /atom

/datum/foobar/meep()
	return /datum/foo

/proc/RunTest()
	return

