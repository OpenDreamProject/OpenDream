
//# issue 137

/proc/lproc(list/list, b)
	ASSERT(list[1] == 2)

/proc/RunTest()
	var/list/list = list(2,3)
	lproc(list, 5)
