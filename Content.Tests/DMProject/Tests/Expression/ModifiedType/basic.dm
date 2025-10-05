
//# issue 473

/datum
	var/a = 5

/proc/RunTest()
	var/datum/D1 = new /datum
	var/datum/D2 = new /datum{a=6}
	ASSERT(D1.a == 5)
	ASSERT(D2.a == 6)