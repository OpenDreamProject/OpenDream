#pragma InvalidReturnType error
/datum/var/mob/bar = new as mob
/mob/var/count = 0 as num
/datum/proc/foo() as num
	return bar.count

/proc/RunTest()
	return
