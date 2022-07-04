// RUNTIME ERROR
#include "Shared/Recursive.dm"

/proc/RunTest()
	var/datum/recursive/R = new()
	var/error = R?.inner.inner