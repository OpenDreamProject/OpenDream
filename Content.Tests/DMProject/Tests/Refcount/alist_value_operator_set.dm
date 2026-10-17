/proc/RunTest()
	var/datum/D = new
	var/alist/mylist = alist()

	mylist["foo"] = D
	ASSERT(refcount(D) == 2)
	mylist -= "foo"
	ASSERT(refcount(D) == 1)