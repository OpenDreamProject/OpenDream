
//# issue 406

/proc/fn()
	return

/proc/RunTest()
	spawn fn()
	return
