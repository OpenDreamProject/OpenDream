/datum/a/var/foo = 12

/proc/RunTest()
	var/datum/a/A = null

	ASSERT(initial(A.foo) == null) // initial() on a null value should return null
