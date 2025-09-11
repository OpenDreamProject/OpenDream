
//# issue 609

/datum
	var/list/L = list(1,2,3,4,5)

/proc/RunTest()
	var/datum/o = new
	ASSERT(issaved(o.L[3]) == FALSE)