/proc/RunTest()
	var/i = 1

	i++
	ASSERT(i == 2)
	i++
	ASSERT(i == 3)

	ASSERT(i++ == 3)
	ASSERT(i == 4)
	ASSERT(++i == 5)
	ASSERT(i == 5)

	i--
	ASSERT(i == 4)
	i--
	ASSERT(i == 3)

	ASSERT(i-- == 3)
	ASSERT(i == 2)
	ASSERT(--i == 1)
	ASSERT(i == 1)

	i = 1.5
	ASSERT(i++ == 1.5)
	ASSERT(i == 2.5)
	ASSERT(i-- == 2.5)
	ASSERT(i == 1.5)
