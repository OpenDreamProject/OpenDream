//COMPILE ERROR
#pragma InvalidReturnType error
/proc/meep(var/foo = "bar" as text) as num
	return foo
	
/proc/RunTest()
	return