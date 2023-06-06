
//# issue 1276

/datum/foo
	var/bar = 5

/proc/RunTest()
	ASSERT((new/datum/foo()).bar == 5)
