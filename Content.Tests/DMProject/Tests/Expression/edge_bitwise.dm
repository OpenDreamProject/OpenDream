
/proc/RunTest()
	ASSERT( (-1 & -1) == 0xFFFFFF )

	ASSERT( (null & 5) == 0)
	ASSERT( (5 & null) == 0)

	ASSERT ( (null | 5) == 5)
	ASSERT ( (null | "abc") == "abc")
	ASSERT ( (5 | null) == 5)

	ASSERT( (5 ^ null) == 5)
	ASSERT( (null ^ 5) == 5)
	ASSERT( (null ^ "abc") == "abc")
	ASSERT( isnull(null ^ null) )

	ASSERT( (~ -42) == 0xFFFFFF)

	ASSERT( (-5 << 1) == 0)
	ASSERT( (5 << -5) == 5)
	ASSERT( (5 << "abc") == 5)

	ASSERT( (5 >> -5) == 5)
	ASSERT( ("abc" >> 5) == 0 )