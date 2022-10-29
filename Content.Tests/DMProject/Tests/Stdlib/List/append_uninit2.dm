
//# issue 478

/proc/RunTest()
	var/thing[]
	thing += list(2)
	ASSERT(islist(thing))
	ASSERT(thing[1] == 2)
