
/proc/foo(a, v = 5)
	return a + v

/proc/RunTest()
	var/b = 0
	ASSERT(foo(2) == 7)
	ASSERT(foo(b) == 5)
	ASSERT(foo(a = 2) == 7)
	ASSERT(foo(a = b) == 5)
	ASSERT(foo(a = b + 2) == 7)
	// foo((b = 5)) // a = b = 5
	ASSERT(foo((b = 5)) == 10) // a = b = 5
	ASSERT(foo((b = 5) + (2)) == 12) // a = (b = 5) + 2
	ASSERT(foo(a = b + 2) == 12)

