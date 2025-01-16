// RUNTIME ERROR

/var/outer1 = "A"

proc/test_outer1()
	outer1 = "B"
	return 3

proc/test_inner1()
	var/static/inner1 = test_outer1()
	return inner1

/proc/do_test()
	test_inner1()

/proc/RunTest()
	do_test()

/mob
	var/static/outer2 = "A"

	proc/test_outer2()
		outer2 = "B"
		return 3

	proc/test_inner2()
		var/static/inner2 = test_outer2()
		return inner2

	verb/test()
		do_test()
		test_inner2()
