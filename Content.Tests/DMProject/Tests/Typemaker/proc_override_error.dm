// COMPILE ERROR OD2701
#pragma InvalidReturnType error
/datum/proc/foo() as num
	return 5

/datum/test/foo()
	return "bar"

/proc/RunTest()
	return
