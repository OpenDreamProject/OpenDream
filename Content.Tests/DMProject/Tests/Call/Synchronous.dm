/proc/sync_test()
	return 1992

/proc/RunTest()
	ASSERT(sync_test() == 1992)