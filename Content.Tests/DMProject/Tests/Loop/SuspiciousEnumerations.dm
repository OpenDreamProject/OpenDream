// COMPILE ERROR
// Test that the pragma works
#pragma SuspiciousAreaContentsEnumeration error
/proc/RunTest()
	var/area/test/my_area
	for(var/item in my_area)
		continue
