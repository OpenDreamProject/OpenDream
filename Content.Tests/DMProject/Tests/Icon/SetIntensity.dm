#include "_helpers.dm"

#define GET_ICON(state) icon('expected_results/SetIntensity.dmi', state)
#define TEST_OPERATION(expected, a...) working_icon = icon(base_icon); working_icon.SetIntensity(a); ASSERT(CompareIcons(working_icon, expected))

/proc/RunTest()
	// note that this icon also includes transparency in each layer
	var/static/icon/base_icon = GET_ICON("")
	var/icon/working_icon

	TEST_OPERATION(base_icon, 1, 1, 1)
	TEST_OPERATION(GET_ICON("red"), 0.5, 1, 1)
	TEST_OPERATION(GET_ICON("green"), 1, 0.5, 1)
	TEST_OPERATION(GET_ICON("blue"), 1, 1, 0.5)
	TEST_OPERATION(GET_ICON("green-blue"), 1, 0.5, 0.5)
	TEST_OPERATION(GET_ICON("red-blue"), 0.5, 1, 0.5)
	TEST_OPERATION(GET_ICON("red-green"), 0.5, 0.5, 1)
	TEST_OPERATION(GET_ICON("dim"), 0.5, 0.5, 0.5)
	TEST_OPERATION(GET_ICON("dark"), 0, 0, 0)


	TEST_OPERATION(GET_ICON("dark"))
	TEST_OPERATION(GET_ICON("dark"), null)
	TEST_OPERATION(GET_ICON("dim"), 0.5)
	TEST_OPERATION(GET_ICON("dim"), 0.5, 0.5)
	TEST_OPERATION(GET_ICON("dim"), 0.5, null, 0.5)

	TEST_OPERATION(base_icon, r=1)
	TEST_OPERATION(GET_ICON("full-green"), g=1)
	TEST_OPERATION(GET_ICON("full-blue"), b=1)

	TEST_OPERATION(base_icon, -1, 1, 1)
	TEST_OPERATION(base_icon, 1, -1, 1)
	TEST_OPERATION(base_icon, 1, -1, -1)