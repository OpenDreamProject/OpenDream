// COMPILE ERROR

//# issue 702

/proc/RunTest()
	var/list/l = list()
	l.type = 2 // cannot change constant value
	ASSERT(l.type != 2)
