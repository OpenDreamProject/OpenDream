
/datum
	var/a = 5
	proc/a()
		ASSERT(a == 5)
		return 5

/proc/RunTest()
	var/datum/o = new
	ASSERT(o.a() == 5)
