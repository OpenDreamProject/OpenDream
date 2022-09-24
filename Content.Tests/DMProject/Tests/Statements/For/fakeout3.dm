
/proc/RunTest()
	var/out = 0
	for (var/x in 1 to 5;)
		out += x
	ASSERT(out == 15)
