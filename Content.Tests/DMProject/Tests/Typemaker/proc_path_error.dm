// COMPILE ERROR
#pragma InvalidReturnType error

/datum/proc/meep() as /atom
	return /atom

/datum/foobar/meep()
	return /datum/foo

/proc/RunTest()
	return

