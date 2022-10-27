/proc/RunTest()
	ASSERT(trunc(5.5) == 5)
	ASSERT(trunc(null) == 0)
	ASSERT(trunc(99.762) == 99)
	ASSERT(trunc(1.#INF) == 0)
	// TODO: Fix this NaN Comparsion
	ASSERT("[trunc(1.#IND)]" == "NaN")