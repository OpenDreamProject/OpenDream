
/datum/proc/foo()
	return __TYPE__

/proc/bar()
	return __TYPE__

/proc/RunTest()
	var/datum/A = new
	ASSERT(A.foo() == /datum)
	ASSERT(isnull(bar()))
