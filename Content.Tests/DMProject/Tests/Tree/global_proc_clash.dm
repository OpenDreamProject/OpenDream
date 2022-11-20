
//# issue 515

/proc/a()
	return 1

/datum/proc/a()
	return 2

/proc/RunTest()
	ASSERT(a() == 1)
	var/datum/d = new
	ASSERT(d.a() == 2)
