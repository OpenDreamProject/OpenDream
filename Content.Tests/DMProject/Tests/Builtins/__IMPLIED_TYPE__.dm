

/datum/test
/proc/RunTest()
	var/datum/test/D = __IMPLIED_TYPE__
	ASSERT(D == /datum/test)
	D = ArgumentTest(__IMPLIED_TYPE__)

/proc/ArgumentTest(some_argument)
	ASSERT(some_argument == /datum/test)