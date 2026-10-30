/proc/RunTest()
	var/datum/D = new
	var/alist/mylist = alist(1 = D)

	mylist.Cut()
	ASSERT(refcount(D) == 1)