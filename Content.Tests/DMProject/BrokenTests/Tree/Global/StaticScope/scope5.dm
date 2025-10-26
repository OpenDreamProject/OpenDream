// TODO: This test needs some more work for it to be usable, as the log in aproc() is sometimes null and sometimes 5

/proc/veryfalse()
	return 0 == 1

/proc/aproc()
	var/static/a = bproc()
	world.log << (a)
	if (veryfalse())
		return a
	else
		return 5

/proc/bproc()
	var/static/b = aproc()
	world.log << (b)
	if (veryfalse())
		return b
	else
		return 5

/proc/RunTest()
	world.log << (aproc())
	world.log << (bproc())
