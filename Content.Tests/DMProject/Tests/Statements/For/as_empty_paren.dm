
//# issue 26

/proc/RunTest()
	var/list/l = list(1,2,3)
	for(var/i as() in l)
		continue
