
/proc/RunTest()
	var/out = 0
	for (var/x = 5 in 1 to 20; x < 10)
		out += x
		ASSERT(x == 5)
		out++
		if(out > 10) break // Infinite loop
	ASSERT(out == 12)
