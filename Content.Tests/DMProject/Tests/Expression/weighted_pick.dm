
//# issue 51

/proc/RunTest()
	var/a = pick(1; "1", 1; "2") // We only care that it gets parsed without erroring
