/datum/test1()
	return

/datum/test2()
	return

/proc/RunTest()
	var/x = null
	ASSERT(x?:test1():test2() == null)
