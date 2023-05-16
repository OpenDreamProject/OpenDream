
//# issue 360

/proc/RunTest()
	var/total = 0
	cont:
		for (var/i in 1 to 5)
			total += i
			continue cont
	ASSERT(total == 1)
