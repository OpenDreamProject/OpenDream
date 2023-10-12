/proc/RunTest()
	ASSERT(arctan(0) == 0)
	ASSERT(arctan(1) == 45)
	ASSERT(arctan(sqrt(3)) == 60)
	ASSERT(round(arctan(3, 4)) == round(53.1301)) //that float precision tho
	ASSERT(arctan(-1, 1) == 135)
	ASSERT(arctan(0, -5) == -90)