// COMPILE ERROR OD0501

//# issue 702

/proc/RunTest()
	var/list/l = list()
	l.type = /list // cannot change constant value
	ASSERT(l.type == /list)
