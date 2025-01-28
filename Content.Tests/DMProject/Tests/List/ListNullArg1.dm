// RUNTIME ERROR

/proc/ListNullArg1(a[5])
	ASSERT(a.len == 5) // a should be null

/proc/RunTest()
	ListNullArg1()
