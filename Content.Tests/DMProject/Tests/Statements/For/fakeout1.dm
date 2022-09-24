
/proc/RunTest()
	var/out = 0
	for (var/x = 2 in 1 to 20; x < 6; x++)
		out += x
	ASSERT(out == 14)
