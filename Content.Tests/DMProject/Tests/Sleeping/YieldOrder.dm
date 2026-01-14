var/counter
#define ExpectOrder(n) ASSERT(++counter == ##n)

/proc/BackgroundSleep(delay, expect)
	set waitfor = FALSE
	sleep(delay)
	world.log << "Expect: [expect]"
	ExpectOrder(expect)

#define MODE_INLINE 0 // spawn
#define MODE_BACKGROUND 1 // set waitfor = FALSE + sleep
#define MODE_RAND 2 // random seeded

#define TestSleep(delay, expect) if(mode == MODE_INLINE || (mode == MODE_RAND && prob(50))){ spawn(##delay) { ExpectOrder(##expect); } } else { BackgroundSleep(##delay, ##expect); }

/proc/TestSequence(mode)
	counter = 0
	var/start_tick = world.time

	TestSleep(0, 2)
	ExpectOrder(1)
	sleep(0)
	ExpectOrder(3)

	TestSleep(-1, 4)
	ExpectOrder(5)

	TestSleep(0, 11)
	sleep(-1)
	ExpectOrder(6)

	TestSleep(-1, 7)
	ExpectOrder(8)
	sleep(-1)
	ExpectOrder(9)

	TestSleep(1, 13)
	sleep(-1)
	ExpectOrder(10)
	sleep(0)
	ExpectOrder(12)

	ASSERT(world.time == start_tick)

	sleep(1)
	ExpectOrder(14)

/proc/RunTest()
	world.log << "Inline:"
	TestSequence(MODE_INLINE)

	world.log << "Background:"
	TestSequence(MODE_BACKGROUND)

	rand_seed(22475)
	for(var/i in 1 to 10000)
		world.log << "Rand-[i]:"
		TestSequence(MODE_RAND)