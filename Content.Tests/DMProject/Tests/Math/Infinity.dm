/var/inf = 1.#INF // Can be encoded in an object var

/proc/RunTest()
	ASSERT(isinf(inf))
	ASSERT(isinf(1.#INF))
	ASSERT(!isinf(null))
	ASSERT(!isinf(5))
