
// issue #2412

/proc/foo(var/bar = 5)
	ASSERT(initial(args)[1] == 6)
/proc/RunTest()
	foo(6)

