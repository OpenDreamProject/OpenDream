
//# issue 689

/datum
	var/a = file("test.dm")

/proc/RunTest()
	var/datum/o = new
	ASSERT(isfile(o.a))
