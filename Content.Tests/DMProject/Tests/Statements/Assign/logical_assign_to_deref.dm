
//# issue 598

/datum
	var/list/v = list(1,2)

/proc/RunTest()
	var/datum/o = new
	o.v ||= list(3,4)
	ASSERT(o.v[1] == 1)