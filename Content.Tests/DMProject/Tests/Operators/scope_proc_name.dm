/datum/proc/foo()
	set name = "abc"
	return

/proc/RunTest()
	ASSERT((/datum/proc/foo::name) == "foo")
