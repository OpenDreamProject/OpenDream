
/proc/RunTest()
	// nulls
	ASSERT((null > 2)		== FALSE)
	ASSERT((null > 1)		== FALSE)
	ASSERT((null > 0)		== FALSE)
	ASSERT((null > -1)		== TRUE)
	ASSERT((null > null)	== FALSE)

	ASSERT((null >= 2)		== FALSE)
	ASSERT((null >= 1)		== FALSE)
	ASSERT((null >= 0)		== TRUE)
	ASSERT((null >= -1)		== TRUE)
	ASSERT((null >= null)	== TRUE)

	ASSERT((null < 2)		== TRUE)
	ASSERT((null < 1)		== TRUE)
	ASSERT((null < 0)		== FALSE)
	ASSERT((null < -1)		== FALSE)
	ASSERT((null < null)	== FALSE)

	ASSERT((null <= 2)		== TRUE)
	ASSERT((null <= 1)		== TRUE)
	ASSERT((null <= 0)		== TRUE)
	ASSERT((null <= -1)		== FALSE)
	ASSERT((null <= null)	== TRUE)

	// nums
	ASSERT((1 > 2)		== FALSE)
	ASSERT((1 > 1)		== FALSE)
	ASSERT((1 > 0)		== TRUE)
	ASSERT((1 > -1)		== TRUE)
	ASSERT((1 > null)	== TRUE)

	ASSERT((1 >= 2)		== FALSE)
	ASSERT((1 >= 1)		== TRUE)
	ASSERT((1 >= 0)		== TRUE)
	ASSERT((1 >= -1)	== TRUE)
	ASSERT((1 >= null)	== TRUE)

	ASSERT((1 < 2)		== TRUE)
	ASSERT((1 < 1)		== FALSE)
	ASSERT((1 < 0)		== FALSE)
	ASSERT((1 < -1)		== FALSE)
	ASSERT((1 < null)	== FALSE)

	ASSERT((1 <= 2)		== TRUE)
	ASSERT((1 <= 1)		== TRUE)
	ASSERT((1 <= 0)		== FALSE)
	ASSERT((1 <= -1)	== FALSE)
	ASSERT((1 <= null)	== FALSE)
