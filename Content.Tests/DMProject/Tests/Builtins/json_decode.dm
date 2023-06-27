
#define JSON(e) json_decode(json_encode(e))
#define TEST(v) ASSERT(JSON(v) == v)

/proc/RunTest()
	TEST(0.0)
	TEST(5)
	ASSERT(isnan(JSON(1.#IND)))
	TEST(-1.#INF)
	TEST(1.#INF)
