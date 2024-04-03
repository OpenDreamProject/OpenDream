#pragma InvalidReturnType error
/datum/proc/foo() as num
	return 5

/proc/bar(datum/D) as num
	return D.foo()

/proc/RunTest()
	var/datum/D = new
	ASSERT(bar(D) == 5)