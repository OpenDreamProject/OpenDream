#pragma AmbiguousProcPath error

/datum/proc/foo()
	return

/datum/foo()
	return

/proc/RunTest()
	var/meep = /datum/foo
	return
