// NaN != NaN in BYOND
#define isnan(x) ( (x) != (x) )

/proc/RunTest()
	ASSERT(text2num(null) == null)
	ASSERT(text2num("") == null)
	ASSERT(text2num(".") == null)
	ASSERT(text2num("-") == null)
	ASSERT(text2num("+") == null)

	ASSERT(text2num("12") == 12)
	ASSERT(text2num("+12") == 12)
	ASSERT(text2num("-12") == -12)
	ASSERT(text2num("0") == 0)
	ASSERT(text2num("1.0") == 1)
	ASSERT(text2num("1.2344") == 1.2344)
	ASSERT(text2num("0.0") == 0)
	ASSERT(text2num(".4") == 0.4)

	ASSERT(text2num("Z", 36) == 35)
	ASSERT(text2num("A", 16) == 10)
	ASSERT(text2num("z", 36) == 35)
	ASSERT(text2num("a", 16) == 10)

	ASSERT(text2num("F.A", 16) == 15.625)
	ASSERT(text2num("f.a", 16) == 15.625)
	ASSERT(text2num("F.A", 15) == null)
	ASSERT(text2num("Z.0", 36) == 35)

	ASSERT(text2num("F..A", 16) == 15)
	ASSERT(text2num("4 2") == 4)

	ASSERT(text2num("0xA") == 10)
	ASSERT(text2num("0xA", 16) == 10)
	ASSERT(text2num("0xA", 15) == 0)
	ASSERT(text2num("0xA", 36) == 1198)

	ASSERT(text2num("nan") == null)
	ASSERT(text2num(" -nansomething") == null)
