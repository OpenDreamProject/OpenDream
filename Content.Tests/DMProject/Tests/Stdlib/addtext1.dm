// COMPILE ERROR

//# issue 663

/proc/RunTest()
	ASSERT(!addtext()) // expected 2 or more args
