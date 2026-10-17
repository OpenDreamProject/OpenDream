// RETURN TRUE

/datum/proc/foo()
	CRASH("unreachable")

/datum/meep/foo()
	. = TRUE
..()

/proc/RunTest()
	var/datum/meep/M = new
	return M.foo()