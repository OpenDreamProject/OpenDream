
/proc/RunTest()
	var/out = 0
	var/a = 1
	var/b = 3
	for (a, a < b, {a++;b++})
		out += a
		out += b
		a++;
	ASSERT(out == 11)
