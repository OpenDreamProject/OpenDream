
//# issue 464

/proc/RunTest()
	var/i = 0
	for()
		if (i > 3) break
		i += 1
	ASSERT(i == 4)
