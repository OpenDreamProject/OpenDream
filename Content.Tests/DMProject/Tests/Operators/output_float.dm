// RETURN TRUE

//# issue 2273

/proc/RunTest()
	var/j = 1
	j << 1 // no-op
	return TRUE


