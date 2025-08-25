/datum/A/var/TestSrcVar = "A"
/datum/A/proc/TestSrcProc()
	return "A"

/datum/B/var/TestSrcVar = "B"
/datum/B/proc/TestSrcProc()
	return "B"

/datum/A/proc/Test()
	ASSERT(TestSrcVar == "A")
	ASSERT(TestSrcProc() == "A")
	src = new /datum/B()
	ASSERT(TestSrcVar == "B")
	ASSERT(TestSrcProc() == "B")

/proc/RunTest()
	var/datum/A/a = new()

	a.Test()