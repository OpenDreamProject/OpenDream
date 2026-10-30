#include "_helpers.dm"
/proc/RunTest()
	var/datum/D = new
	var/datum/other_datum = new
	var/list/mylist = list("a" = (D))
	var/list/listb = list("a" = (other_datum))

	BYOND_POP_CACHE
	ASSERT(refcount(D) == 2)
	ASSERT(refcount(other_datum) == 2)

	mylist += listb

	BYOND_POP_CACHE
	ASSERT(refcount(D) == 1)
	ASSERT(refcount(other_datum) == 3)
