// COMPILE ERROR OD2402

#pragma DanglingSyntax error

/proc/RunTest()
	var/datum/D = new
	var/foo = D.
