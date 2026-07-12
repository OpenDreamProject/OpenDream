#include "_helpers.dm"

#define GET_ICON(state) icon('expected_results/SwapColor.dmi', state)
#define TEST_OPERATION(expected, a...) working_icon = icon(base_icon); working_icon.SwapColor(a); ASSERT(CompareIcons(working_icon, expected))

/proc/RunTest()
	var/static/icon/base_icon = GET_ICON("")
	var/icon/working_icon

	TEST_OPERATION(GET_ICON("flat-swap"), "#f00", "#0f0")
	TEST_OPERATION(GET_ICON("max-swap"), "#f00f", "#0f0")
	TEST_OPERATION(GET_ICON("semi-swap"), "#f00a", "#0f0")
	TEST_OPERATION(GET_ICON("semi-swap-alpha"), "#f00a", "#0f0a")
	TEST_OPERATION(GET_ICON("full-swap"), "#f00", "transparent")

	TEST_OPERATION(GET_ICON("flat-swap"), "#f00", "#0f0a") // if replace's alpha isn't 0, we don't care
	TEST_OPERATION(GET_ICON("full-swap"), "#f00", "#0f00") // if replace's alpha IS 0, we wipe