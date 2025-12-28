/proc/async_return()
	. = 420
	sleep(0)
	return 1337

/proc/RunTest()
	ASSERT(async_return() == 1337)