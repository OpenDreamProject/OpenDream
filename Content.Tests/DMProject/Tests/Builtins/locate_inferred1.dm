/datum/foo
/proc/RunTest()
	var/datum/foo/F = locate()
	ASSERT(isnull(F))