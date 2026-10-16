#include "noop.dm"
/proc/RunTest()
	var/datum/D = new
	var/list/mylist = new /list(1)

	mylist[1] = D
	BYOND_POP_CACHE
	ASSERT(refcount(D) == 2)

	mylist -= D
	BYOND_POP_CACHE
	ASSERT(refcount(D) == 1)