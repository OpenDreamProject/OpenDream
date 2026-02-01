// RETURN TRUE

var/should_be_set = FALSE

/proc/RunTest()
    ASSERT(world.time == 0)
    sleep(world.tick_lag)
    ASSERT(world.time == 1)
    sleep(10)
    ASSERT(world.time == 11)
    StackCheck()
    ASSERT(world.time == 61)
    ASSERT(should_be_set)
    return TRUE

/proc/StackCheck()
    ASSERT(world.time == 11)
    sleep(50)
    ASSERT(world.time == 61)
    should_be_set = TRUE
