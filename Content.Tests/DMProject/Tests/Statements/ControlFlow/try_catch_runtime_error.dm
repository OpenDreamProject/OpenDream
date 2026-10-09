// Runtime errors caught by try/catch become an /exception with the file and line of the error
var/crash_line

/proc/NestedCrash()
	var/list/L = null
	global.crash_line = __LINE__ + 1
	L.Add(1)

/proc/RunTest()
	var/list/L = null
	var/expected_line = __LINE__ + 2
	try
		L.Add(1)
	catch(var/exception/e)
		ASSERT(istype(e, /exception))
		ASSERT(e.file == __FILE__)
		ASSERT(e.line == expected_line)

	try
		NestedCrash()
	catch(var/exception/e2)
		ASSERT(istype(e2, /exception))
		ASSERT(e2.file == __FILE__)
		ASSERT(e2.line == global.crash_line)
