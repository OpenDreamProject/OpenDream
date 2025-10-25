/proc/RunTest()
	// Note byond goes counterclockwise (why) (unlike every other turn proc also)

	// Handle the testable invalid case
	ASSERT(turn(0, 0) == 0)

	// Make sure it returns right
	ASSERT(turn(EAST, 0) == EAST)
	ASSERT(turn(WEST, 0) == WEST)
	ASSERT(turn(NORTH, 0) == NORTH)
	ASSERT(turn(SOUTH, 0) == SOUTH)
	ASSERT(turn(NORTHWEST, 0) == NORTHWEST)
	ASSERT(turn(SOUTHWEST, 0) == SOUTHWEST)
	ASSERT(turn(NORTHEAST, 0) == NORTHEAST)
	ASSERT(turn(SOUTHEAST, 0) == SOUTHEAST)

	// Lets try flipping them
	ASSERT(turn(EAST, 180) == WEST)
	ASSERT(turn(WEST, 180) == EAST)
	ASSERT(turn(NORTH, 180) == SOUTH)
	ASSERT(turn(SOUTH, 180) == NORTH)
	ASSERT(turn(NORTHWEST, 180) == SOUTHEAST)
	ASSERT(turn(SOUTHWEST, 180) == NORTHEAST)
	ASSERT(turn(NORTHEAST, 180) == SOUTHWEST)
	ASSERT(turn(SOUTHEAST, 180) == NORTHWEST)

	// By 90s this time
	ASSERT(turn(EAST, 90) == NORTH)
	ASSERT(turn(WEST, 90) == SOUTH)
	ASSERT(turn(NORTH, 90) == WEST)
	ASSERT(turn(SOUTH, 90) == EAST)
	ASSERT(turn(NORTHWEST, 90) == SOUTHWEST)
	ASSERT(turn(SOUTHWEST, 90) == SOUTHEAST)
	ASSERT(turn(NORTHEAST, 90) == NORTHWEST)
	ASSERT(turn(SOUTHEAST, 90) == NORTHEAST)

	// *Clockwise* 90s
	ASSERT(turn(EAST, -90) == SOUTH)
	ASSERT(turn(WEST, -90) == NORTH)
	ASSERT(turn(NORTH, -90) == EAST)
	ASSERT(turn(SOUTH, -90) == WEST)
	ASSERT(turn(NORTHWEST, -90) == NORTHEAST)
	ASSERT(turn(SOUTHWEST, -90) == NORTHWEST)
	ASSERT(turn(NORTHEAST, -90) == SOUTHEAST)
	ASSERT(turn(SOUTHEAST, -90) == SOUTHWEST)

	ASSERT(turn(WEST, 22) == WEST)
	ASSERT(turn(WEST, 44) == WEST)
	ASSERT(turn(WEST, 75) == SOUTHWEST)

	ASSERT(turn(WEST, -22) == WEST)
	ASSERT(turn(WEST, -44) == WEST)
	ASSERT(turn(WEST, -75) == NORTHWEST)
