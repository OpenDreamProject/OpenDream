
//# issue 360

/proc/RunTest()
	var/total = 7
	goto cont
	total = 0
	cont:
		for (var/i in 1 to 2)
			for(;;)
				total += i
				if(total < 5)
					continue
				continue cont
	ASSERT(total == 10)
