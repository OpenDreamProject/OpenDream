
//# issue 360

/proc/RunTest()
	var/total = 0
	outer:
		for (var/i in list(1,100))
			for(;;)
				total += i
				if(total > 5)
					break outer
	ASSERT(total == 6)
