

/datum/test/var/bar = "foobar"
/proc/RunTest()
	var/datum/test/D = __IMPLIED_TYPE__
	ASSERT(D.bar == "foobar")

