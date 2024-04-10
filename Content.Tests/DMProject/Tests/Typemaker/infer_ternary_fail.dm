// COMPILE ERROR
#pragma InvalidVarType error
/proc/foo() as text
	var/foo = "hi" as text
	var/bar = 10 as num
	return prob(50) ? foo : bar