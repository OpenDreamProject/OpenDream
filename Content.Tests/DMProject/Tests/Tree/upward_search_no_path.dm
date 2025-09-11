/datum/foo

/proc/RunTest()
	var/bar = /datum/foo.
	ASSERT(bar == /datum/foo)
