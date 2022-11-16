// COMPILE ERROR

/proc/one()
	return 1

var/static/a = 1 / 0 - one()

/proc/RunTest()
	return