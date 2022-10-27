
#define TEST_1(x) abc_##x

// The whitespace around the '##' should be ignored
#define TEST_2(x) def_ ## x

/proc/RunTest()
	var/TEST_1(def) = 10
	var/TEST_2(abc) = 20

	ASSERT(abc_def == 10)
	ASSERT(def_abc == 20)
