//# issue 616

/datum
	var/const/b = 5

/proc/RunTest()
	var/datum/o = new
	var/const/v = o.b
	ASSERT(v == 5)
