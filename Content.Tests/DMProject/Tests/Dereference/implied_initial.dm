
/datum/var/bar = "foobar"
/proc/RunTest()
	var/datum/D = /datum
	ASSERT(D.bar == "foobar")