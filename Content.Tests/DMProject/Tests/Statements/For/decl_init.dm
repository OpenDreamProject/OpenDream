
/proc/RunTest()
	var/a = 0
	for (var/i, i < 5; i++)
		a += i
	ASSERT(a == 10)
