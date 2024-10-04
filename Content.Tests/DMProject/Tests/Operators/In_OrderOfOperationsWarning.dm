// COMPILE ERROR OD3204

#pragma AmbiguousInOrder error

/proc/RunTest()
	if ("a" in list("a") && FALSE)
		return TRUE