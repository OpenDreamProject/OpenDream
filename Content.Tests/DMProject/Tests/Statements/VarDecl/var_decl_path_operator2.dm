// COMPILE ERROR OD3206

#pragma SuspiciousPathOperator error

/datum/var/foo = FALSE
/datum/child/foo = TRUE

/proc/RunTest()
	var/datum/child.C = new
	return C.foo
