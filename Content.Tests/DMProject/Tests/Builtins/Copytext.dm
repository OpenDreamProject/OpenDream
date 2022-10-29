
/proc/RunTest()
	var/test_string = "wubba lubba dub dub}"
	var/arbitrary_start = 4
	var/lena = length(test_string[arbitrary_start])
	var/lenb = -length(copytext_char(test_string, -1))
	test_string = copytext(test_string, arbitrary_start + lena, -1)
	ASSERT(test_string == "a lubba dub dub")
	ASSERT(lena == 1)
	ASSERT(lenb == -1)
