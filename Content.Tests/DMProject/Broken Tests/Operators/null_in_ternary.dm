
//# issue 513

/proc/RunTest()
	var/a = 1 ? 2 : ()
	var/b = 1 ? () : 1
	ASSERT(a == 2)
	ASSERT(b == null)
