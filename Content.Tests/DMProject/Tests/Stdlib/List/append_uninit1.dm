
//# issue 478

/proc/RunTest()
	var/thing[]
	thing += 2
	ASSERT(thing == 2)
