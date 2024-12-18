// RUNTIME ERROR

/proc/ListNullArg2(a[5][3])
	ASSERT(a[1].len == 3) // a should be null

/proc/RunTest()
	ListNullArg2()
