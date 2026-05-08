// TODO: This test needs some more work for it to be usable, as the log in aproc() is sometimes null and sometimes 5
var/count = 0

/proc/veryfalse()
	return 0 == 1

/proc/aproc()
	var/static/a = bproc()
	count += a
	if (veryfalse())
		return a
	else
		return 5

/proc/bproc()
	var/static/b = aproc()
	count += b
	if (veryfalse())
		return b
	else
		return 5

/proc/RunTest()
	ASSERT(aproc() == 5)
	ASSERT(bproc() == 5)
	ASSERT(count == 15)
