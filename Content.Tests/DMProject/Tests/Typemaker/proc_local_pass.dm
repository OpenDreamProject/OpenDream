// NOBYOND
#pragma InvalidReturnType error
/datum/proc/foo() as num
	var/const/meep = 5
	return meep

/proc/RunTest()
	return
