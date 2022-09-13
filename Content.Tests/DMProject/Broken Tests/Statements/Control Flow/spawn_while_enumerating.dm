
//# issue 163

/proc/update()
	return

/proc/RunTest()
	var/list/L = list(1,2,3)
	for(var/i in L)
		spawn(0)
			update()

