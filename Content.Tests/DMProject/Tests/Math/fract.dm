/proc/RunTest()
	ASSERT(fract(6) == 0)
	ASSERT(fract(1.5) == 0.5)
	ASSERT(fract(-1.5) == -0.5)
	ASSERT(fract(null) == 0)
	ASSERT(fract(99.5) == 0.5)
	ASSERT(fract(1.#INF) == 0)
	// TODO: Fix this NaN Comparsion
	ASSERT(isnan(trunc(1.#IND)))