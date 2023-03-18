/proc/RunTest()
	world << text2ascii_char("abcdéfg",6)
	ASSERT(text2ascii_char("abcdéfg",6) == 102) //102 is f