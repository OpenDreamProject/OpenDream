
//# issue 204

/proc/RunTest()
	var/list/l = newlist(/datum, /datum, /datum)
	ASSERT(l[2].type == /datum)
