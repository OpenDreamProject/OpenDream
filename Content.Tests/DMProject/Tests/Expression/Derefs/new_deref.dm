
//# issue 1276

/datum/foo
	var/bar = 5

/proc/RunTest()
	var/datum/foo/a
	ASSERT((a = new/datum/foo()).bar == 5)

	var/b
	ASSERT((b = new/datum/foo().bar) == 5)

	ASSERT(new/datum/foo().bar == 5)
