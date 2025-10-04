
/proc/RunTest()
	// null violates the holy trichotomy here, with how it compares to an empty string.
	ASSERT( (null > "") == 0 )
	ASSERT( (null >= "") == 1 )
	ASSERT( (null == "") == 0 )
	ASSERT( (null <= "") == 1 )
	ASSERT( (null < "") == 0 )

	ASSERT( (null > 5) == 0 )
	ASSERT( (null > -5) == 1 )
	ASSERT( (null > "abc") == 0 )
	ASSERT( (null > null) == 0 )

	ASSERT( (null < 5) == 1 )
	ASSERT( (null < -5) == 0 )
	ASSERT( (null < "abc") == 1 )
	ASSERT( (null < null) == 0 )

	ASSERT( (0.15 <= null) == 0)
	ASSERT( (null >= 0.5) == 0)
	ASSERT( (-0.3 >= null) == 0)