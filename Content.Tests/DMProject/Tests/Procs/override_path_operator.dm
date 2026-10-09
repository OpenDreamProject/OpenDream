// RETURN TRUE

/datum/proc/foo()
	return FALSE
/datum/child.foo()
	return TRUE

/proc/RunTest()
	var/datum/child/C = new
	return C.foo()
