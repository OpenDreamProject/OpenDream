
/datum/subclass/v = 5
/datum/var/v = 6

/proc/RunTest()
	var/datum/subclass/o = new
	var/datum/da = new
	ASSERT(o.v == 5)
	ASSERT(da.v == 6)
