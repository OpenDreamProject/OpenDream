
//# issue 666

/datum/o
	var/test = 5

/proc/get_obj()
	return (new /datum/o)

/proc/RunTest()
	var/datum/o/thing
	var/c = (thing = get_obj()).test
	ASSERT(c == 5)