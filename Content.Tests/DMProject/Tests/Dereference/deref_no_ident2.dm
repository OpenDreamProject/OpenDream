// COMPILE ERROR OD0001

// Dangling '?.' errors but '.' doesn't
/proc/RunTest()
	var/datum/D = new
	var/foo = D?.
