/proc/ArgEquals1(val1)
	ASSERT(ispath(val1, /obj))
	return TRUE

/proc/ArgEquals2(val1, val2)
	ASSERT(ispath(val1, /obj))
	ASSERT(isnull(val2))
	return TRUE

/proc/ArgEquals3(val1, val2, val3)
	ASSERT(ispath(val1, /obj))
	ASSERT(val2 == 6)
	ASSERT(isnull(val3))
	return TRUE

/proc/RunTest()
	 ASSERT(ArgEquals1(/obj = 2) == TRUE)
	 ASSERT(ArgEquals2(/obj = 2) == TRUE)
	 ASSERT(ArgEquals3(/obj = 2, 6) == TRUE)
