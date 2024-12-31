// COMPILE ERROR OD0011

/proc/one()
	return 1

var/static/a = 1 / 0 - one()

/proc/RunTest()
	return