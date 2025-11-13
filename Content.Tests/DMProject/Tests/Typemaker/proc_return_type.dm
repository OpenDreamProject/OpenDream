// NOBYOND
#pragma InvalidReturnType error
/datum/proc/foo() as num
	return 5

/datum/test/foo()
	return 10

/proc/RunTest()
	return
