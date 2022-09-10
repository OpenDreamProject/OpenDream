
/proc/eqarg1(val1)
	ASSERT(ispath(val1, /obj))

/proc/eqarg2(val1, val2)
	ASSERT(ispath(val1, /obj))
	ASSERT(isnull(val2))

/proc/RunTest()
	eqarg1(/obj = 2)
	eqarg2(/obj = 2)
