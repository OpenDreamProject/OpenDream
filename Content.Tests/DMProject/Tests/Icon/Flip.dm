#include "_helpers.dm"

#define GET_ICON(state) icon('expected_results/Flip.dmi', state)
#define TEST_OPERATION(expected, a...) working_icon = icon(base_icon); working_icon.Flip(a); ASSERT(CompareIcons(working_icon, expected))

/proc/RunTest()
	var/static/icon/base_icon = GET_ICON("")
	var/static/icon/vertical = GET_ICON("v")
	var/static/icon/horizontal = GET_ICON("h")
	var/static/icon/full_flip = GET_ICON("hv")
	var/icon/working_icon

	TEST_OPERATION(vertical, NORTH)
	TEST_OPERATION(vertical, SOUTH)
	TEST_OPERATION(horizontal, EAST)
	TEST_OPERATION(horizontal, WEST)
	TEST_OPERATION(full_flip, NORTHEAST)
	TEST_OPERATION(full_flip, SOUTHWEST)

	TEST_OPERATION(base_icon, (NORTH | SOUTH))
	TEST_OPERATION(base_icon, (EAST | WEST))
	TEST_OPERATION(base_icon, UP)
	TEST_OPERATION(base_icon, DOWN)
	TEST_OPERATION(base_icon, (NORTH | UP))
	TEST_OPERATION(base_icon, (NORTH | DOWN))
	TEST_OPERATION(base_icon, NORTHWEST) //???
	TEST_OPERATION(base_icon, SOUTHEAST) //???