// COMPILE ERROR

//# issue 172

/proc/a()
	return 1

/proc/a()
	return 2

/proc/RunTest()
	a()
