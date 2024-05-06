//COMPILE ERROR
#pragma InvalidReturnType error
/proc/foo()
	return "bar"

/proc/bar() as text
	return foo()

/proc/RunTest()
	return