// COMPILE ERROR OD3206

#pragma SuspiciousPathOperator error

/datum/proc/foo()
	return FALSE
/datum/child.foo()
	return TRUE

/proc/RunTest()
	var/datum/child/C = new
	return C.foo()
