
/proc/RunTest()
	var/a = 0
	for(var/i = i, i < 6, i++)
		a += i
	ASSERT(a == 15)
