// RUNTIME ERROR

/proc/ListNullArg1(a[5])
	ASSERT(a.len == 5)

/proc/RunTest()
	ListNullArg1()
