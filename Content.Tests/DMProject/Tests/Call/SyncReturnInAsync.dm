/proc/sync_return()
	. = 420
	sleep(0)
	return 1337

/proc/RunTest()
	ASSERT(sync_return() == 420)