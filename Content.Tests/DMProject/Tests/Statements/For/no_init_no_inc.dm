
//# issue 332

/proc/RunTest()
	var/i = 3
	for(,i > 0,)
		i--
	ASSERT(i == 0)
