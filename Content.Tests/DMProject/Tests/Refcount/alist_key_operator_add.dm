#include "noop.dm"
/proc/RunTest()
	var/datum/D = new
	var/alist/mylist = alist()

	mylist += D
	BYOND_POP_CACHE
	ASSERT(refcount(D) == 2)

	mylist -= D
	BYOND_POP_CACHE
	ASSERT(refcount(D) == 1)