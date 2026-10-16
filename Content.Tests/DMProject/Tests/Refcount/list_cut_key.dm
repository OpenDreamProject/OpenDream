/proc/RunTest()
	var/datum/D = new
	var/list/mylist = list(D)

	mylist.Cut()
	ASSERT(refcount(D) == 1)