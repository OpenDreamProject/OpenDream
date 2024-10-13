//COMPILE ERROR OD3101
//Test that our pragma for this is working.
#pragma EmptyProc error

/proc/foo()
	set waitfor = false // Set statements shouldn't count.

/proc/RunTest()
	foo()
