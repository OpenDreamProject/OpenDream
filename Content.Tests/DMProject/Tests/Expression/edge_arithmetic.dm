
/proc/RunTest()
	ASSERT( (-null) == 0 )
	ASSERT( (-"str") == 0 )

	ASSERT( (1.0 ** "abc") == 1 )
	ASSERT( (1.0 ** null) == 1 )
	ASSERT( (null ** "abc") == 1 )
	ASSERT( (null ** 1.0) == 0 )

	ASSERT( (null - null) == 0 )

	ASSERT( (null * null) == 0 )
	ASSERT( (null * 5) == 0 )
	ASSERT( (null * "abc") == 0 )

	ASSERT( (5 * null) == 0)

	ASSERT( (5 / null) == 5 )
	ASSERT( (5 / "abc") == 5 )

	ASSERT( (null % 5) == 0 )