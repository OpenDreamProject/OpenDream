#define MACRO(ARG_1, ARG_2) "[#ARG_1] [##ARG_2]"

/proc/RunTest()
	ASSERT(MACRO(1, 2) == "1 2")