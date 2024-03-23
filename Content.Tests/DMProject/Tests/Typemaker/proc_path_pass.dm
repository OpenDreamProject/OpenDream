#pragma InvalidReturnType error
/datum/foo
/datum/proc/meep() as /datum/foo
	return new /datum/foo

/datum/foobar/meep()
	return new /datum/foo

/proc/RunTest()
	return
