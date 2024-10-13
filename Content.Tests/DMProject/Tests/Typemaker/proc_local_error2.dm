// COMPILE ERROR OD2701
#pragma InvalidReturnType error
/datum/proc/foo() as num
	var/meep = 5 // Not const
	return meep

/proc/RunTest()
	return
