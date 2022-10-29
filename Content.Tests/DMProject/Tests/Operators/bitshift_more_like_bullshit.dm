
/proc/RunTest()
	//Normal bitshifting
	ASSERT((1 << 2) == 4)
	ASSERT((1 << 2.5) == 4)
	ASSERT((4 >> 2) == 1)

	//The silly part
	ASSERT((2 << "seven") == 2)
	ASSERT(("two" << 7) == 0)
	ASSERT(("two" << "seven") == 0)

	ASSERT((4 >> 2) == 1)
	ASSERT(("four" >> 2) == 0)
	ASSERT((4 >> "two") == 4)
	ASSERT(("four" >> "two") == 0)
