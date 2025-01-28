/datum/var/name

/datum/thing_1/dodaa
	name = "underscore 1 test"

#define NUMPATH_OBJDEF(num) /datum/thing_##num/name = #num

NUMPATH_OBJDEF(4)
NUMPATH_OBJDEF(stuff)

/proc/RunTest()
	var/datum/thing_1/dodaa/D = new
	ASSERT(D.name == "underscore 1 test")
	var/datum/thing_4/T = new
	ASSERT(T.name == "4")
	var/datum/thing_stuff/Y = new
	ASSERT(Y.name == "stuff")