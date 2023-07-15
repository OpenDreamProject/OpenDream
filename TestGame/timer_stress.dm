// This simulates SS13 startup with a CHECK_TICK-like mechanism for avoiding overrunning the tick.
// Fake work is generated and the throughput is reported every second.
/proc/TimerStress()
	set waitfor = FALSE
	world.fps = 10

	var/iter_count = 0;
	var/last_time = 0;
	while (TRUE)
		for (var/i = 0; i < 1000; i++)
			Nop()
		
		iter_count += 1;
		if (world.time - last_time > 10)
			world.log << "iterations: [iter_count]"
			iter_count = 0
			last_time = world.time

		if (world.tick_usage > 98)
			sleep(world.tick_lag)

/proc/Nop()
	return
