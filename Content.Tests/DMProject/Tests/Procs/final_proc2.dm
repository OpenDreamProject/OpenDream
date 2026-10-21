/proc/RunTest()
    return

// "final" is only a keyword directly after "proc"; below it's an ordinary name.
// The procs named "final" have to come before /datum/test/final exists as a type, otherwise BYOND
// reads the override as a redefinition of that type (issue 2648).
/datum/proc/foo()
	return
/datum/test/foo()
	return
/datum/proc/final()
	return
/datum/test/final()
	return
/datum/test/final/foo()
	return
/datum/test/again/foo()
	return
