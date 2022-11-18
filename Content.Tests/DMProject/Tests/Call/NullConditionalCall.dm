/datum/test1()
	return

/datum/test2()
	return

/proc/RunTest()
	var/x = null
	ASSERT(x?:test1():test2() == null)

	var/list/L = list(1, 2, 3, list(4, 5, 6), null)
	var/list/L2 = null
	
	ASSERT(L?[1] == 1)
	ASSERT(L2?[CRASH("uh-oh")] == null)
	ASSERT(L?[4][1] == 4)
	ASSERT(L[5]?[1] == null)