/proc/RunTest()
	var/datum/D = new
	var/alist/mylist = alist((D) = 1)

	mylist.Cut()
	ASSERT(refcount(D) == 1)