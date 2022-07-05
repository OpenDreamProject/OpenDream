/obj/A/var/TestSrcVar = "A"
/obj/A/proc/TestSrcProc()
	return "A"

/obj/B/var/TestSrcVar = "B"
/obj/B/proc/TestSrcProc()
	return "B"

/obj/A/proc/Test()
	ASSERT(TestSrcVar == "A")
	ASSERT(TestSrcProc() == "A")
	src = new /obj/B()
	ASSERT(TestSrcVar == "B")
	ASSERT(TestSrcProc() == "B")

/proc/RunTest()
	var/obj/A/a = new()

	a.Test()