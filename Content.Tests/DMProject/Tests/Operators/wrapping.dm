
/datum/foo
	var/bar = 5

/proc/RunTest()
	ASSERT((new/datum/foo()).bar == 5)

/world/New()
	RunTest()
