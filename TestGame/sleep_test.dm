// This tests that two sleep(-1) timers correctly interact.
/proc/sleep_test()
	world.fps = 10
	spawn(0)
		sleep(1)
		while (TRUE)
			world.log << "A"
			sleep(-1)

	spawn(0)
		sleep(1)
		while (TRUE)
			world.log << "B"
			sleep(-1)

	sleep(1)
	world.log << "ACK"
