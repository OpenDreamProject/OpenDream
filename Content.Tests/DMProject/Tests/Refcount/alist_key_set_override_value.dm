#include "_helpers.dm"
/proc/RunTest()
	var/datum/key = new
	var/datum/value = new
	var/alist/mylist = alist((key) = (value))

	BYOND_POP_CACHE
	ASSERT(refcount(key) == 2)
	ASSERT(refcount(value) == 2)

	mylist[key] = "foo"

	BYOND_POP_CACHE
	ASSERT(refcount(key) == 2)
	ASSERT(refcount(value) == 1)
