/proc/RunTest()
	var/val = 0xBEEF
	ASSERT(val << 13 == 14540800)
	val = -1
	ASSERT(val >> 13 == 2047)

	//check the const folded version
	ASSERT(0xBEEF << 13 == 14540800)
	ASSERT(-1 >> 13 == 2047)