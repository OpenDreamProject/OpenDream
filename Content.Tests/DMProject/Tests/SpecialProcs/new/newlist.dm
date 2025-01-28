
//# issue 204

/proc/RunTest()
	var/list/l = newlist(/obj, /obj, /obj)
	ASSERT(l[2].type == /obj)
