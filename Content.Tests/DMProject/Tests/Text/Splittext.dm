/proc/GetFailMessage(var/test_pair)
	var/O_str_got = ""
	var/O_str_expected = ""
	for (var/i in test_pair[1])
		O_str_got += "\"[i]\", "
	for (var/i in test_pair[2])
		O_str_expected += "\"[i]\", "
	return "Got:\n[O_str_got]\nexpected:\n[O_str_expected]\n"

/proc/RunTest()
	var/test_text = "The average of 1, 2, 3, 4, 5 is: 3"
	// A list of pairs of lists which is just the compiled Splittext implementation vs. the DM implementation output.
	// To add your own test just create a new element in here of list(splittext(your_args), list(output_dm_gave_for_that_call))
	// Note that as of writing, regex with capturing groups behaves **very** weirdly in DM, and splittext() does not yet replicate that behavior. Other regexes should be ok.
	var/list/tests = list(
		list(splittext(test_text, regex(@"\d"), 5, 30, 1), list("The average of ","1",", ","2",", ","3",", ","4",", ","5"," is: 3")),
		list(splittext(test_text, regex(@"\d"), 5, 30), list("The average of ",", ",", ",", ",", "," is: 3")),
		list(splittext(test_text, regex("")), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3","")),
		list(splittext(test_text, "", 0, 10, 1), list()),
		list(splittext(test_text, regex(""), 0, 0, 1), list("The average of 1, 2, 3, 4, 5 is: 3")),
		list(splittext(test_text, ""), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3")),
		list(splittext(test_text, "", 5, 10, 1), list("The a","","v","","e","","r","","a","","ge of 1, 2, 3, 4, 5 is: 3")),
		list(splittext(test_text, "", 1, length(test_text)+1, 1), list("T","","h","","e",""," ","","a","","v","","e","","r","","a","","g","","e",""," ","","o","","f",""," ","","1","",",",""," ","","2","",",",""," ","","3","",",",""," ","","4","",",",""," ","","5",""," ","","i","","s","",":",""," ","","3")),
		list(splittext(test_text, regex(""), 5, 10, 1), list("The a","","v","","e","","r","","age of 1, 2, 3, 4, 5 is: 3")),
		// list(splittext(test_text, regex("()"), 1, length(test_text)+1, 1), list("T","","","h","","","e","",""," ","","","a","","","v","","","e","","","r","","","a","","","g","","","e","",""," ","","","o","","","f","",""," ","","","1","","",",","",""," ","","","2","","",",","",""," ","","","3","","",",","",""," ","","","4","","",",","",""," ","","","5","",""," ","","","i","","","s","","",":","",""," ","","","3")),
		list(splittext(test_text, regex(@"")), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3","")),
		list(splittext(test_text, " "), list("The","average","of","1,","2,","3,","4,","5","is:","3")),
		list(splittext(test_text, " ", 5), list("The average","of","1,","2,","3,","4,","5","is:","3")),
		list(splittext(test_text, " ", 5, 10), list("The average of 1, 2, 3, 4, 5 is: 3")),
		list(splittext(test_text, " ", 10, 20), list("The average","of","1,","2, 3, 4, 5 is: 3")),
		list(splittext(test_text, " ", 10, 20, 1), list("The average"," ","of"," ","1,"," ","2, 3, 4, 5 is: 3")),
		list(splittext(test_text, regex(@"\d")), list("The average of ",", ",", ",", ",", "," is: ","")),
		// list(splittext(test_text, regex(@"(\d)"), 1, length(test_text)+1, 1), list("The average of ","1","1",", ","2","2",", ","3","3",", ","4","4",", ","5","5"," is: ","3","3",""))
	)
	// Go through all our tests and ensure that the test results match the expected output
	var/t_num = 0
	var/tests_failed = 0
	var/fail_output = "\n"
	for (var/test_pair in tests)
		t_num += 1
		// Special debug output for failed tests
		if (!(test_pair[1] ~= test_pair[2]))
			tests_failed = 1
			fail_output += "Failed test [t_num]:\n[GetFailMessage(test_pair)]"

	if (tests_failed)
		CRASH(fail_output)	

		// list(splittext(test_text, regex("")), "splittext(test_text, regex(\"\"))"),
		// list(splittext(test_text, "", 0, 10, 1), "splittext(test_text, \"\", 0, 10, 1)"),
		// list(splittext(test_text, regex(""), 0, 0, 1), "splittext(test_text, regex(\"\"), 0, 0, 1)"),
		// list(splittext(test_text, ""), "splittext(test_text, \"\")"),
		// list(splittext(test_text, "", 5, 10, 1), "splittext(test_text, \"\", 5, 10, 1)"),
		// list(splittext(test_text, "", 1, length(test_text)+1, 1), "splittext(test_text, \"\", 1, length(test_text)+1, 1)"),
		// list(splittext(test_text, regex(""), 5, 10, 1), "splittext(test_text, regex(\"\"), 5, 10, 1)"),
		// list(splittext(test_text, regex("()"), 1, length(test_text)+1, 1), "splittext(test_text, regex(\"()\"), 1, length(test_text)+1, 1)"),
		// list(splittext(test_text, regex(@"")), "splittext(test_text, regex(@\"\"))"),
		// list(splittext(test_text, " "), "splittext(test_text, \" \")"),
		// list(splittext(test_text, " ", 5), "splittext(test_text, \" \", 5)"),
		// list(splittext(test_text, " ", 5, 10), "splittext(test_text, \" \", 5, 10)"),
		// list(splittext(test_text, " ", 10, 20), "splittext(test_text, \" \", 10, 20)"),
		// list(splittext(test_text, " ", 10, 20, 1), "splittext(test_text, \" \", 10, 20, 1)"),
		// list(splittext(test_text, regex(@"\d")), "splittext(test_text, regex(@\"\\d\"))"),
		// list(splittext(test_text, regex(@"\d"), 5, 30), "splittext(test_text, regex(@\"\\d\"), 5, 30)"),
		// list(splittext(test_text, regex(@"\d"), 5, 30, 1), "splittext(test_text, regex(@\"\\d\"), 5, 30, 1)"),

	// var/list/tests = list(
	// 	list(splittext(test_text, regex("")), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3","")),
	// 	list(splittext(test_text, "", 0, 10, 1), list()),
	// 	list(splittext(test_text, regex(""), 0, 0, 1), list("The average of 1, 2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, ""), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3")),
	// 	list(splittext(test_text, "", 5, 10, 1), list("The a","","v","","e","","r","","a","","ge of 1, 2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, "", 1, length(test_text)+1, 1), list("T","","h","","e",""," ","","a","","v","","e","","r","","a","","g","","e",""," ","","o","","f",""," ","","1","",",",""," ","","2","",",",""," ","","3","",",",""," ","","4","",",",""," ","","5",""," ","","i","","s","",":",""," ","","3")),
	// 	list(splittext(test_text, regex(""), 5, 10, 1), list("The a","","v","","e","","r","","age of 1, 2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, regex("()"), 1, length(test_text)+1, 1), list("T","","","h","","","e","",""," ","","","a","","","v","","","e","","","r","","","a","","","g","","","e","",""," ","","","o","","","f","",""," ","","","1","","",",","",""," ","","","2","","",",","",""," ","","","3","","",",","",""," ","","","4","","",",","",""," ","","","5","",""," ","","","i","","","s","","",":","",""," ","","","3")),
	// 	list(splittext(test_text, regex(@"")), list("T","h","e"," ","a","v","e","r","a","g","e"," ","o","f"," ","1",","," ","2",","," ","3",","," ","4",","," ","5"," ","i","s",":"," ","3","")),
	// 	list(splittext(test_text, " "), list("The","average","of","1,","2,","3,","4,","5","is:","3")),
	// 	list(splittext(test_text, " ", 5), list("The average","of","1,","2,","3,","4,","5","is:","3")),
	// 	list(splittext(test_text, " ", 5, 10), list("The average of 1, 2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, " ", 10, 20), list("The average","of","1,","2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, " ", 10, 20, 1), list("The average"," ","of"," ","1,"," ","2, 3, 4, 5 is: 3")),
	// 	list(splittext(test_text, regex(@"\d")), list("The average of ",", ",", ",", ",", "," is: ","")),
	// 	list(splittext(test_text, regex(@"\d"), 5, 30), list("The average of ",", ",", ",", ",", "," is: 3")),
	// 	list(splittext(test_text, regex(@"\d"), 5, 30, 1), list("The average of ","1",", ","2",", ","3",", ","4",", ","5"," is: 3")),
	// )


// var/test_text = "The average of 1, 2, 3, 4, 5 is: 3"
// var/list/tests = list(
// 	list(splittext(test_text, regex(@"\d")), "splittext(test_text, regex(@\"\\d\"))"),
// 	list(splittext(test_text, regex(@"\d"), 5, 30), "splittext(test_text, regex(@\"\\d\"), 5, 30)"),
// 	list(splittext(test_text, regex(@"\d"), 5, 30, 1), "splittext(test_text, regex(@\"\\d\"), 5, 30, 1)"),
// )
// for (var/test in tests)
// 	var/delimiter = "\",\""
// 	world.log << "list([test[2]], list(\"[jointext(test[1], delimiter)]\")),"