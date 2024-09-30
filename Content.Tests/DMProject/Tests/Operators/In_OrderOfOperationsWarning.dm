// COMPILE ERROR

#pragma AmbiguousInOrder error

/proc/RunTest()
	if ("a" in list("a") && FALSE)
		return TRUE