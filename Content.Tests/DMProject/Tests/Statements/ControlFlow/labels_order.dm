
//# issue 360

/proc/RunTest()
	var/thing = 0
	goto there
	thing = 100
	there:
	ASSERT(thing == 0)
