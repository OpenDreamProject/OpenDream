#pragma InvalidReturnType error
/datum/foo
/datum/proc/meep() as /datum
	return /datum

/datum/foobar/meep()
	return /datum/foo // Don't error since it's a subtype

/proc/RunTest()
	return
