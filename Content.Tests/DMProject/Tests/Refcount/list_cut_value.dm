/proc/RunTest()
	var/datum/D = new
	var/list/mylist = list("foo" = D)

	mylist.Cut()
	ASSERT(refcount(D) == 1)