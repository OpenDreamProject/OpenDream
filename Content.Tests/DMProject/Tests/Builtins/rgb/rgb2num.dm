#define COLORCOMPARE(rgb, r, g, b) ASSERT((rgb[1] == r) && (rgb[2] == g) && (rgb[3] == b))

/proc/RunTest()
	COLORCOMPARE(rgb2num(null), 255, 255, 255)
	COLORCOMPARE(rgb2num("#fff"), 255, 255, 255)
	COLORCOMPARE(rgb2num("#f00"), 255, 0, 0)
	COLORCOMPARE(rgb2num("#00f"), 0, 0, 255)
	COLORCOMPARE(rgb2num("#ffffff"), 255, 255, 255)
	COLORCOMPARE(rgb2num("#ff0000"), 255, 0, 0)
	COLORCOMPARE(rgb2num("#0000ff"), 0, 0, 255)
	COLORCOMPARE(rgb2num("#12f5e6"), 18, 245, 230)
	COLORCOMPARE(rgb2num("#ca60db", COLORSPACE_RGB), 202, 96, 219)
#ifdef OPENDREAM
	COLORCOMPARE(rgb2num("#ca60db", COLORSPACE_HSV), 291.70734, 56.164383, 85.882355)
	COLORCOMPARE(rgb2num("#ca60db", COLORSPACE_HSL), 291.70734, 63.07692, 61.764706)
	//COLORCOMPARE(rgb2num("#ca60db", COLORSPACE_HCY), 291.70734, 68.2215, 55.423534) // TODO Support HCY
#endif