//COMPILE ERROR OD2701
// NOBYOND
#pragma InvalidReturnType error
/proc/meep(var/foo = "bar" as text) as num
	return foo
	
/proc/RunTest()
	return