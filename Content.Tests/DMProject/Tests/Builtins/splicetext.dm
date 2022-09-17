/proc/RunTest()
    CRASH(splicetext("banana", 3, 6, "laclav"))
	ASSERT(splicetext("banana", 3, 6, "laclav") == "balaclava")