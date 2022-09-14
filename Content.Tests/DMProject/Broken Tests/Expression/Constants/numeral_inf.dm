
//# issue 464

/proc/RunTest()
	var/a = 1#INF
	var/b = -1#IND
	// TODO: This actually varies by OS I believe, this is the Windows result. (OpenDream is broken either way)
	ASSERT(a == "inf")
	ASSERT(b == 0)
