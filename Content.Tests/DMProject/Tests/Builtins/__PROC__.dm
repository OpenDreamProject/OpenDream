
/datum/proc/foo()
	return __PROC__

/proc/bar()
	return __PROC__

/proc/RunTest()
	var/datum/A = new
	ASSERT(A.foo() == /datum/proc/foo)
	ASSERT(bar() == /proc/bar)
