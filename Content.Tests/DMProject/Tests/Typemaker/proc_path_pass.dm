#pragma InvalidReturnType error
/datum/foo
/datum/proc/meep() as /datum/foo
	return /datum/foo

/datum/foobar/meep()
	return /datum/foo

/proc/RunTest()
	return
