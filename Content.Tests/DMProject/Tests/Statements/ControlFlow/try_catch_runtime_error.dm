// Runtime errors caught by try/catch become an /exception with the file and line of the error
var/crash_line

/proc/NestedCrash()
	global.crash_line = __LINE__ + 1
	CRASH("nested")

/proc/RunTest()
	var/expected_line = __LINE__ + 2
	try
		CRASH("hi")
	catch(var/exception/e)
		ASSERT(istype(e, /exception))
		ASSERT(e.name == "hi")
		ASSERT(e.file == __FILE__)
		ASSERT(e.line == expected_line)

	try
		NestedCrash()
	catch(var/exception/e2)
		ASSERT(istype(e2, /exception))
		ASSERT(e2.name == "nested")
		ASSERT(e2.line == global.crash_line)
