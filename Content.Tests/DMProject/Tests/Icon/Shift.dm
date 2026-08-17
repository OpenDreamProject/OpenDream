#include "_helpers.dm"

#define GET_ICON(state) icon('expected_results/Shift.dmi', state)
#define TEST_OPERATION(expected, a...) working_icon = icon(base_icon); working_icon.Shift(a); ASSERT(CompareIcons(working_icon, expected))
#define NONE 0

/proc/RunTest()
	var/static/icon/base_icon = GET_ICON("")
	var/icon/working_icon

	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1)
	TEST_OPERATION(GET_ICON("south-1"), SOUTH, 1)
	TEST_OPERATION(GET_ICON("east-1"), EAST, 1)
	TEST_OPERATION(GET_ICON("west-1"), WEST, 1)
	TEST_OPERATION(GET_ICON("northeast-1"), NORTHEAST, 1)
	TEST_OPERATION(GET_ICON("northwest-1"), NORTHWEST, 1)
	TEST_OPERATION(GET_ICON("southeast-1"), SOUTHEAST, 1)
	TEST_OPERATION(GET_ICON("southwest-1"), SOUTHWEST, 1)

	// wraps
	TEST_OPERATION(GET_ICON("north-1-wrap"), NORTH, 1, TRUE)
	TEST_OPERATION(GET_ICON("south-1-wrap"), SOUTH, 1, TRUE)
	TEST_OPERATION(GET_ICON("east-1-wrap"), EAST, 1, TRUE)
	TEST_OPERATION(GET_ICON("west-1-wrap"), WEST, 1, TRUE)

	//full wraparounds
	TEST_OPERATION(GET_ICON("north-1-wrap"), NORTH, 33, TRUE)
	TEST_OPERATION(GET_ICON("south-1-wrap"), SOUTH, 33, TRUE)
	TEST_OPERATION(GET_ICON("east-1-wrap"), EAST, 33, TRUE)
	TEST_OPERATION(GET_ICON("west-1-wrap"), WEST, 33, TRUE)
	TEST_OPERATION(base_icon, NORTH, 32, TRUE)
	TEST_OPERATION(base_icon, SOUTH, 32, TRUE)
	TEST_OPERATION(base_icon, EAST, 32, TRUE)
	TEST_OPERATION(base_icon, WEST, 32, TRUE)

	// edge cases
	TEST_OPERATION(base_icon, NONE)
	TEST_OPERATION(base_icon, NONE, 1)
	TEST_OPERATION(base_icon, NORTH)
	TEST_OPERATION(base_icon, NORTH, 0)
	TEST_OPERATION(base_icon, NORTH|SOUTH, 1)
	TEST_OPERATION(base_icon, EAST|WEST, 1)
	TEST_OPERATION(base_icon, UP|NORTH, 1)
	TEST_OPERATION(base_icon, DOWN|EAST, 1)
	TEST_OPERATION(GET_ICON("north-1-wrap"), NORTH, 1, -1) // wrap checks if the integer value isn't 0
	TEST_OPERATION(GET_ICON("north-1-wrap"), NORTH, 1, 2) // wrap checks if the integer value isn't 0
	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1, 0.5)
	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1, -0.5)
	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1, "true") // it does not check for truthy!
	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1, new /datum)
	TEST_OPERATION(GET_ICON("north-1"), NORTH, 1, null)