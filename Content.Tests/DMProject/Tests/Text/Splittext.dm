/proc/list_equal_asert(list/A, list/B)
	ASSERT(length(A) == length(B))
	for (var/i = 1; i <= length(A); i++)
		ASSERT(A[i] == B[i])

/proc/RunTest()
	var/test_text = "The average of 1, 2, 3, 4, 5 is: 3"
	var/list/test1 = splittext(test_text, " ")
	var/list/test1_expected = list("The","average","of","1,","2,","3,","4,","5","is:","3")
	list_equal_asert(test1, test1_expected)

	var/list/test2 = splittext(test_text, " ", 5)
	var/test2_expected = list("average","of","1,","2,","3,","4,","5","is:","3")
	list_equal_asert(test2, test2_expected)

	var/list/test3 = splittext(test_text, " ", 5, 10)
	var/test3_expected = list("avera")
	list_equal_asert(test3, test3_expected)

	var/list/test4 = splittext(test_text, " ", 10, 20)
	var/test4_expected = list("ge","of","1,","2")
	list_equal_asert(test4, test4_expected)

	var/list/test5 = splittext(test_text, " ", 10, 20, 1)
	var/test5_expected = list("ge"," ","of"," ","1,"," ","2")
	list_equal_asert(test5, test5_expected)

	//it's regex time
	var/test6 = splittext(test_text, regex(@"\d"))
	var/test6_expected = list("The average of ",", ",", ",", ",", "," is: ","")
	list_equal_asert(test6, test6_expected)

	var/test7 = splittext(test_text, regex(@"\d"), 5, 30)
	var/test7_expected = list("average of ",", ",", ",", ",", "," ")
	list_equal_asert(test7, test7_expected)

	var/test8 = splittext(test_text, regex(@"\d"), 5, 30, 1)
	var/test8_expected = list("average of ","1",", ","2",", ","3",", ","4",", ","5"," ")
	list_equal_asert(test8, test8_expected)