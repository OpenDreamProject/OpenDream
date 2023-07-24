//COMPILE ERROR
//Test that our pragma for this is working.
#pragma EmptyProc error

/proc/foo()
	set waitfor = false // Set statements shouldn't count.

/proc/RunTest()
	foo()
