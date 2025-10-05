
//# issue 473

/datum
	var/a = 5
	var/b = 7

/proc/RunTest()
	var/datum/D1 = new /datum
	var/datum/D2 = new /datum{a=6;b=8}
	ASSERT(D1.a == 5)
	ASSERT(D1.b == 7)
	ASSERT(D2.a == 6)
	ASSERT(D2.b == 8)