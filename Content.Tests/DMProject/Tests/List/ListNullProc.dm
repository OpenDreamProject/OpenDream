/proc/RunTest()
	var/a[]
	var/b[5][3]

	ASSERT(!islist(a))
	ASSERT(islist(b))
	ASSERT(islist(b[1]))
	ASSERT(b[1].len == 3)
