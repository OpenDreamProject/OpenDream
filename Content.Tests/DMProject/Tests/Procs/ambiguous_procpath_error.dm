// COMPILE ERROR OD2601
#pragma AmbiguousProcPath error

/datum/proc/foo()
	return

/datum/foo()
	return

/proc/RunTest()
	var/meep = /datum/proc/foo
	return
