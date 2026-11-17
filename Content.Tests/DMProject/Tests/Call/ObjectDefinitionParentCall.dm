/datum
	..()
	var/meep = "foo"

/proc/RunTest()
	var/datum/D = new
	ASSERT(D.meep == "foo")
