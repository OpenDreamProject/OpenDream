// RETURN TRUE

// This test confirms we don't OD0404 if a var is set to a procpath that does not contain the "/proc/" element

/atom/movable/proc/foo()
	return

/datum/var/bar = /atom/movable/foo 

/proc/RunTest()
	return TRUE
