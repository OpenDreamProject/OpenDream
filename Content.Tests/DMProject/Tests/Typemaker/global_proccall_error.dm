//COMPILE ERROR OD2701
#pragma InvalidReturnType error
/proc/foo()
	return "bar"

/proc/bar() as text
	return foo()

/proc/RunTest()
	return