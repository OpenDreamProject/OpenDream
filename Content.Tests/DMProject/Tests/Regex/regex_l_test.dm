
/proc/RunTest()
	var/regex/l_simple = regex(@"\l")
	var/regex/l_complex_1 = regex(@"\\\\l")
	var/regex/l_complex_2 = regex(@"\\\\l")

	var/l_simple_match = l_simple.Find("0abc123def")
	var/l_complex_match_1 = l_complex_1.Find("1234")
	var/l_complex_match_2 = l_complex_2.Find(@"\\\\l")
	ASSERT(l_simple_match == 2)
	ASSERT(l_complex_match_1 == 0)
	// Unsure why but this gives 4 even though the pattern string is reduced down to {\\l} / "\\\\l"
	// Seems to be matching {\l} / "\\l"
	ASSERT(l_complex_match_2 == 3)
