
/proc/RunTest()
	ASSERT(sign(5.2) == 1)
	ASSERT(sign(-5.2) == -1)
	ASSERT(sign(0) == 0)
	ASSERT(sign(null) == 0)
	ASSERT(sign("") == 0)
	ASSERT(sign("foo") == 0)
	ASSERT(sign(list(1)) == 0)
	