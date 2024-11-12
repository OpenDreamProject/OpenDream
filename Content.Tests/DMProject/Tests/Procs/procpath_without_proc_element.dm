// COMPILE ERROR OD0404

/atom/movable/proc/foo()
	return

// This only works without the "proc" element if an override is declared
/datum/var/bar = /atom/movable/foo 

/proc/RunTest()
	return
