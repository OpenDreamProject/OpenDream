
//# issue 361

var/counter = 0
var/counter_updated = FALSE

/world/Tick()
	ASSERT(!counter_updated)
	var/updated = ++counter
	counter_updated = TRUE
	sleep(world.tick_lag)
	// this should run before the next world.Tick()
	ASSERT(!counter_updated)
	ASSERT(updated == counter)

/proc/RunTest()
	sleep(world.tick_lag) // at time of writing, initial call to DreamThread.Run happens before the first tick and it's fucky
	counter_updated = FALSE
	for(var/i in 1 to 100)
		var/last_read = counter
		sleep(-1)
		ASSERT(!counter_updated)
		ASSERT(last_read == counter)
		sleep(0)
		ASSERT(!counter_updated)
		ASSERT(last_read == counter)
		sleep(world.tick_lag)
		var/expected = last_read + 1
		var/actual = counter
		ASSERT(counter_updated)
		if(expected != actual)
			CRASH("Expected: [expected] Actual: [actual]!")
		counter_updated = FALSE
