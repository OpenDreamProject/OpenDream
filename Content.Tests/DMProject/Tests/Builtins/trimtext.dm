/proc/RunTest()
	ASSERT(trimtext("   test") == "test")
	ASSERT(trimtext("test   ") == "test")
	ASSERT(trimtext("   test   ") == "test")
