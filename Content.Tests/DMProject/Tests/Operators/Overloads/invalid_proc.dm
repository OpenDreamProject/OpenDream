// COMPILE ERROR

//This results in an undefined proc error in BYOND also

/datum/tester
	proc/operator_()
		return 555

/proc/RunTest()
	var/datum/tester = new()
	ASSERT(tester.operator_() != 555)
