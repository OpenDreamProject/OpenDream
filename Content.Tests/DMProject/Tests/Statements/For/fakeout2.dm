
/proc/RunTest()
	for (var/x in 1 to 5;;)
		ASSERT(!isnum(x))
		break; // This is an infinite loop
