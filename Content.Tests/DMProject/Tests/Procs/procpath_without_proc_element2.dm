// RETURN TRUE

/atom/movable/proc/foo()
	return

/atom/movable/foo()
	return

// This only works without the "proc" element if an override is declared
/datum/var/bar = /atom/movable/foo 

/proc/RunTest()
	return TRUE
