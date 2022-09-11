
//# issue 26

// FIXME: This seems to get stuck in an infinite loop or something in OD

/proc/RunTest()
	var/list/l = list(1,2,3)
	for(var/i as() in l)
		continue