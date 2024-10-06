// COMPILE ERROR OD2701
#pragma InvalidReturnType error
/datum/proc/foo() as num
	var/const/meep = "foo"
	return meep

/proc/RunTest()
	return
