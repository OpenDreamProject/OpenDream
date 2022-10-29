
//# issue 477

/proc/RunTest()
	var/list/thing = new /list(2)
	ASSERT(length(thing) == 2)
