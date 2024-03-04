// COMPILE ERROR
#pragma ImplicitNullType error
/proc/do(var/meep as num) // Only error if the arg is not a subtype of datum/
	return

/proc/RunTest()
	return
