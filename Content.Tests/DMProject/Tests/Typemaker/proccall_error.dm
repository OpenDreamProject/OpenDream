//COMPILE ERROR
#pragma InvalidReturnType error
/datum/proc/foo()
	return "bar"

/datum/proc/bar() as text
	return foo()

/proc/RunTest()
	return