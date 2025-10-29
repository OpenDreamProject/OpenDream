#include "Shared/Recursive.dm"

/proc/RunTest()
	var/datum/recursive/R = null
	R?.val *= CRASH("this shouldn't be evaluated")
	R = new()

	ASSERT((R?.val *= 2) == 4)