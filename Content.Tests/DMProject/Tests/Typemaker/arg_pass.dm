#pragma InvalidReturnType error
/proc/meep(var/foo = "bar" as text) as text
	return foo
	
/proc/RunTest()
	return