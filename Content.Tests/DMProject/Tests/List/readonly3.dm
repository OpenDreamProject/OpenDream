// COMPILE ERROR OD0501

//# issue 702

/proc/RunTest()
	var/list/l = list()
	l.type = /obj // cannot change constant value
	ASSERT(l.type != /obj)
