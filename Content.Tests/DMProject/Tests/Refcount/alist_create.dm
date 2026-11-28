#include "_helpers.dm"
/proc/RunTest()
	var/datum/key = new
	var/datum/value = new
	var/alist/myalist = alist((key) = (value))

	BYOND_POP_CACHE
	ASSERT(refcount(key) == 2)
	ASSERT(refcount(value) == 2)
