
//# issue 163

/proc/update()
	return

/proc/RunTest()
	for(var/i = 0; i < 2; i++)
		spawn(0) update()

