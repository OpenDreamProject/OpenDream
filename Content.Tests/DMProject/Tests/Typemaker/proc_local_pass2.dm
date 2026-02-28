// NOBYOND
#pragma InvalidReturnType error
/datum/proc/foo() as num
	var/meep = 5 as num
	return meep

/proc/RunTest()
	return
