
/datum/proc/G()
	return 0

/proc/G()
	return 1

/proc/RunTest()
	ASSERT(G() == 1)