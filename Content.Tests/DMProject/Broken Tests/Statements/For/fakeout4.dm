

/proc/RunTest()
	for (var/x in 1 to 20; out < 10)
		ASSERT(!isnum(x))
		break // Infinite loop
