/proc/RunTest()
	var/test_text = "The average of 1, 2, 3, 4, 5 is: 3"
	var/list/test1 = splittext(test_text, " ")
	var/list/test1_expected = list("The","average","of","1,","2,","3,","4,","5","is:","3")
	ASSERT(test1 ~= test1_expected)

	var/list/test2 = splittext(test_text, " ", 5)
	var/test2_expected = list("average","of","1,","2,","3,","4,","5","is:","3")
	ASSERT(test2 ~= test2_expected)

	var/list/test3 = splittext(test_text, " ", 5, 10)
	var/test3_expected = list("avera")
	ASSERT(test3 ~= test3_expected)

	var/list/test4 = splittext(test_text, " ", 10, 20)
	var/test4_expected = list("ge","of","1,","2")
	ASSERT(test4 ~= test4_expected)

	var/list/test5 = splittext(test_text, " ", 10, 20, 1)
	var/test5_expected = list("ge"," ","of"," ","1,"," ","2")
	ASSERT(test5 ~= test5_expected)

	//it's regex time
	var/test6 = splittext(test_text, regex(@"\d"))
	var/test6_expected = list("The average of ",", ",", ",", ",", "," is: ","")
	ASSERT(test6 ~= test6_expected)

	var/test7 = splittext(test_text, regex(@"\d"), 5, 30)
	var/test7_expected = list("average of ",", ",", ",", ",", "," ")
	ASSERT(test7 ~= test7_expected)

	var/test8 = splittext(test_text, regex(@"\d"), 5, 30, 1)
	var/test8_expected = list("average of ","1",", ","2",", ","3",", ","4",", ","5"," ")
	ASSERT(test8 ~= test8_expected)