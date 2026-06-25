// NOBYOND
#pragma InvalidReturnType error
/proc/foo() as text
	return "bar"

/proc/bar() as text
	return foo()

/proc/RunTest()
	return