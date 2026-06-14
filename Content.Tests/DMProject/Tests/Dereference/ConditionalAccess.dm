#include "Shared/Recursive.dm"

/proc/RunTest()
	var/datum/recursive/R = new()
	R?.inner?.inner = CRASH("this shouldn't be evaluated")
	R.inner = 1

	ASSERT(R?.inner == 1)