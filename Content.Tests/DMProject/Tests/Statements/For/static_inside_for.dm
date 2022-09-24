
/proc/RunTest()
	var/i = 0
	var/out = 0
	for(i = 1; i < 4; i++)
		var/static/s = 0
		s += i
		out += s
	ASSERT(out == 10)
