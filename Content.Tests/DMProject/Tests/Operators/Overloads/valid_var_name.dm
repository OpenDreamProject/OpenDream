/datum/tester
	proc/operator_()
		return 555

	proc/operator()
		return 54321

/proc/RunTest()
	var/operator = 1234
	ASSERT(operator == 1234)
	var/datum/tester/T = new()
	ASSERT(T.operator_() == 555)
	ASSERT(T.operator() == 54321)