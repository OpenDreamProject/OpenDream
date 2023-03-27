// COMPILE ERROR
// Test that the pragma works
#pragma SuspiciousListNew error
/proc/RunTest()
	var/list/L = list()
	L += new
