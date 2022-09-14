
/proc/RunTest()
	var/out = 0

	for (var/a in 2 to 8 step 3)
		out += a
	
	ASSERT(out == 15)

	for (var/a = 1 in 2 to 8 step 3)
		out += a

	ASSERT(out == 30)
