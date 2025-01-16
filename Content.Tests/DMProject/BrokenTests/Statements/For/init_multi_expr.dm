
/proc/RunTest()
	var/out = 0
	for ({var/a=1;var/b=3}, a < b, a++)
		out += a

	ASSERT(out == 3)