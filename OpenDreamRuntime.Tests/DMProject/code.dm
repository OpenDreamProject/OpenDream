/proc/sync_test()
	return 1992

/world/proc/async_test()
	sleep(1)
	return 1337

/world/proc/error_test()
	. = 1
	src:nonexistent_proc()
	. = 2

/world/proc/image_test()
	return image('a', "Hello")

/world/proc/crash_test()
	. = 1
	CRASH("This should stop the current proc")
	. = 2

/world/New()
	..()
	world.log << "World loaded!"
