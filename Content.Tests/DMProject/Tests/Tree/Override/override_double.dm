
/datum/proc/A()
	return 1

/datum/A()
	return 2

/datum/A()
	return 3

var/datum/a = new

/proc/RunTest()
	ASSERT(a.A() == 3)
