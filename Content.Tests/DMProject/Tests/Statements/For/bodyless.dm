
//# issue 527

/proc/RunTest()
	var/A;
	for(A = 0; A < 10, A++);
	ASSERT(A == 10)
