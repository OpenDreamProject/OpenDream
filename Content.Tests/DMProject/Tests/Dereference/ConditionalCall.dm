// IGNORE
// This test case is currently broken
#include "Shared/Recursive.dm"

/proc/RunTest()
	var/datum/recursive/R = new()
	var/result = R?.inner?.get_inner(CRASH("this shouldn't be evaluated"))

	ASSERT(result == null)