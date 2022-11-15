// COMPILE ERROR

/datum/proc/G()
	return 0

/proc/G()
	return 1

G()
	return 2

/proc/RunTest()
	return