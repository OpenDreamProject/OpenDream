
//# issue 655
//# issue 265

/datum/proc/nullproc(null, temp)
	ASSERT(isnull(null))
	ASSERT(temp == 2)

/proc/RunTest()
	var/datum/D = new
	D.nullproc(1,2)
