/proc/RunTest()
	ASSERT(text2ascii_char("abcdéfg",6) == 102) //102 is f
	ASSERT(ascii2text(text2ascii("a")) == "a")