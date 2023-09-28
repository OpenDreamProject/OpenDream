/datum/proc/test1()
	return

/datum/proc/test2()
	return

/proc/RunTest()
	var/x = null
	ASSERT(x?:test1():test2() == null)

	var/list/L = list(1, 2, 3, list(4, 5, 6), null)
	var/list/L2 = null
	
	ASSERT(L?[1] == 1)
	ASSERT(L2?[CRASH("index should not evaluate")] == null)
	ASSERT(L?[4][1] == 4)
	ASSERT(L[5]?[1] == null)

	L2?[1] = CRASH("rhs should not evaluate")
	L2?[1] += CRASH("rhs should not evaluate")