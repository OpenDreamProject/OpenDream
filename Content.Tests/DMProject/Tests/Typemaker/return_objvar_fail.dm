// COMPILE ERROR
#pragma InvalidReturnType error
/datum/var/bar = 5 as num

/datum/proc/meep() as text
	var/datum/D = new /datum
	return D.bar

/proc/RunTest()
	return
