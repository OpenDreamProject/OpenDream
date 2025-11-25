//COMPILE ERROR OD3100
// NOBYOND
//Test that our pragma for this is working.
#pragma EmptyBlock error
/proc/RunTest()
	var/thing = 2
	if(TRUE)
	var/other_thing = 3
