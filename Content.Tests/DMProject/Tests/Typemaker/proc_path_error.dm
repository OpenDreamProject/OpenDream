// COMPILE ERROR
#pragma InvalidReturnType error
/datum/foo
/datum/proc/meep() as /datum
	return new /datum

/datum/foobar/meep()
	return new /datum/foo

/proc/RunTest()
	return
