
//# issue 429

/proc/RunTest()
	var/thing = 2
	weirdness:
		if(thing % 2 == 0)
			thing = 3
			goto weirdness
	ASSERT(thing == 3)
