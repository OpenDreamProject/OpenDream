
/proc/RunTest()
	var/regex/l_simple = regex(@"\l")
	var/regex/l_complex_1 = regex(@"\\\\l")
	var/regex/l_complex_2 = regex(@"\\\\l")
	var/regex/L_simple = regex(@"\L")

	var/l_simple_match = l_simple.Find("0abc123def")
	var/l_complex_match_1 = l_complex_1.Find("1234")
	var/l_complex_match_2 = l_complex_2.Find(@"\\\\l")
	var/L_simple_match = L_simple.Find("abc123")

	ASSERT(l_simple_match == 2)
	ASSERT(l_complex_match_1 == 0)
	ASSERT(l_complex_match_2 == 3)
	ASSERT(L_simple_match == 4)
