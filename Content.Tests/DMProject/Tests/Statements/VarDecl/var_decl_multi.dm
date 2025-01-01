
//# issue 507

/proc/RunTest()
	var/{a=1;b=2;c=3;d=4}
	ASSERT(a == 1)
	ASSERT(b == 2)
	ASSERT(c == 3)
	ASSERT(d == 4)
