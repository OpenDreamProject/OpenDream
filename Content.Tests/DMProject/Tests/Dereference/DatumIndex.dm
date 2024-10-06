// COMPILE ERROR OD2304
#pragma InvalidIndexOperation error

// Indexing a datum (e.g. datum["foo"]) is not valid in BYOND 515.1641+

/proc/RunTest()
	var/datum/meep = new
	ASSERT(isnull(meep["foo"]))
