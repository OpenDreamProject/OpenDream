/datum/proc/foo()
	return

/proc/RunTest()
	ASSERT((/datum/proc/foo::name) == "foo")
