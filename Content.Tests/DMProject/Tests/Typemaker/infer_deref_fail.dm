// COMPILE ERROR
#pragma InvalidReturnType error
/datum/var/mob/bar = new as mob
/mob/var/thing/count
/datum/proc/foo() as mob
	return bar?.count

/proc/RunTest()
	return
