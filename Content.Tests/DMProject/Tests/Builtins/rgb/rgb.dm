/proc/RunTest()
	ASSERT(rgb(255, 255, 255) =="#ffffff")
	ASSERT(rgb(255, 0, 0) == "#ff0000" )
	ASSERT(rgb(0, 0, 255) == "#0000ff")
	ASSERT(rgb(18, 245, 230) == "#12f5e6")
	ASSERT(rgb(18, 245, 230, 128) == "#12f5e680")
	ASSERT(rgb(202, 96, 219, space=COLORSPACE_RGB) == "#ca60db")
	ASSERT(rgb(291.70734, 56.164383, 85.882355, space=COLORSPACE_HSV) == "#ca60db")
	ASSERT(rgb(291.70734, 63.07692, 61.764706, space=COLORSPACE_HSL) == "#ca60db" )

	ASSERT(rgb(291.70734, 63.07692, 61.764706, 128, COLORSPACE_HSL) == "#ca60db80" )
	//ASSERT(rgb(291.70734, 68.2215, 55.423534, space=COLORSPACE_HCY) == "#ca60db") // TODO Support HCY

	ASSERT(rgb(r=1, g=2, b=3) == "#010203")
	ASSERT(rgb(b=3, g=2, r=1) == "#010203")
	ASSERT(rgb(radical=1, goblin=2, baddies=3) == "#010203")
	ASSERT(rgb(r=1, 2, 3) == "#010203")
