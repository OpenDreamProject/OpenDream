
/datum/test1/proc/meep()
	ASSERT(FALSE)

/datum/test2/proc/meep()
	return 5

/datum/test3/proc/meep()
	ASSERT(FALSE)

/proc/RunTest()
	var/datum/test1/T1 = new()
	var/datum/test2/T2 = new()
	var/datum/test3/T3 = new()
	ASSERT((T1 ? T2 : T3).meep() == 5)
