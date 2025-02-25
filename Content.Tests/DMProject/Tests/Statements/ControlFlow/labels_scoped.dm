
//# issue 360

/proc/RunTest()
	var/thing = 0
	if(1)
		weirdness:
		if(thing % 2 == 0)
			thing = 3
			goto weirdness
		ASSERT(thing == 3)
	if(1)
		weirdness:
		if(thing % 3 == 0)
			thing = 7
			goto weirdness
		ASSERT(thing == 7)
