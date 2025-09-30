
/datum/proc/foo()
	ASSERT(nameof(__TYPE__) == "datum")

/proc/RunTest()
	var/datum/D = new
	D.foo()
