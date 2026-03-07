// NOBYOND
#pragma InvalidReturnType error
/datum/proc/foo() as text
	return "bar"

/datum/proc/bar() as text
	return foo()

/proc/RunTest()
	return