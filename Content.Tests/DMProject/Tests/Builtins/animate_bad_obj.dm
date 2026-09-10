// RUNTIME ERROR

/proc/RunTest()
	var/object = "not an object"
	return animate(object, time = 1)
