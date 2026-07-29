#include "_helpers.dm"

#define GET_ICON(state) icon('expected_results/MapColors.dmi', state)
#define TEST_OPERATION(expected, a...) working_icon = icon(base_icon); working_icon.MapColors(a); ASSERT(CompareIcons(working_icon, expected))

/proc/RunTest()
	var/static/icon/base_icon = GET_ICON("")
	var/icon/working_icon

	var/static/icon/expected_greyscale = GET_ICON("greyscale")
	TEST_OPERATION(expected_greyscale, 0.299,0.299,0.299,	0.587,0.587,0.587,	0.114,0.114,0.114)
	TEST_OPERATION(expected_greyscale, 0.299,0.299,0.299,	0.587,0.587,0.587,	0.114,0.114,0.114,	0,0,0)
	TEST_OPERATION(expected_greyscale, 0.299,0.299,0.299,0,	0.587,0.587,0.587,0,	0.114,0.114,0.114,0,	0,0,0,1,	0,0,0,0)

	var/static/icon/expected_moonlight = GET_ICON("moonlight")
	TEST_OPERATION(expected_moonlight, 0.2,0.05,0.05,	0.1,0.3,0.2,	0.1,0.1,0.4)
	TEST_OPERATION(expected_moonlight, 0.2,0.05,0.05,	0.1,0.3,0.2,	0.1,0.1,0.4, 0,0,0)
	TEST_OPERATION(expected_moonlight, 0.2,0.05,0.05,0,	0.1,0.3,0.2,0,	0.1,0.1,0.4,0,	0,0,0,1,	0,0,0,0)
	
	var/static/icon/expected_invert = GET_ICON("invert")
	TEST_OPERATION(expected_invert, -1,0,0,	0,-1,0,	0,0,-1,	1,1,1)
	TEST_OPERATION(expected_invert, -1,0,0,0,	0,-1,0,0,	0,0,-1,0,	0,0,0,1,	1,1,1,0)

	// RGBA format without specifying transparency will remove transparency from that color
	TEST_OPERATION(GET_ICON("malformed-r"), "#4C4C4C", "#96969600", "#1D1D1D00", "#000000ff", "#00000000")
	TEST_OPERATION(GET_ICON("malformed-g"), "#4C4C4C00", "#969696", "#1D1D1D00", "#000000ff", "#00000000")
	TEST_OPERATION(GET_ICON("malformed-b"), "#4C4C4C00", "#96969600", "#1D1D1D", "#000000ff", "#00000000")
	TEST_OPERATION(GET_ICON("malformed-a"), "#4C4C4C00", "#96969600", "#1D1D1D00", "#000000", "#00000000")
	TEST_OPERATION(GET_ICON("malformed-0"), "#4C4C4C00", "#96969600", "#1D1D1D00", "#000000ff", "#000000")