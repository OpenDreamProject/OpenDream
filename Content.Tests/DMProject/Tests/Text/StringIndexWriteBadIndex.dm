// RUNTIME ERROR

/proc/RunTest()
	var/value = "abc"
	value["1"] = "x"
	return value

