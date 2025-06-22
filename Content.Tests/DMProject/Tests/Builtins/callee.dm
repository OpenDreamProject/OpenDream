/proc/bar()
	set desc = "bar description"
	
	ASSERT(callee.name == "bar")
	ASSERT(callee.desc == "bar description")
	return callee

/proc/RunTest()
	ASSERT(callee.name == "RunTest")
	ASSERT(callee.file == "callee.dm")
	
	var/callee/expired_callee = bar()
	var/failed = FALSE
	try
		var/name = expired_callee.name
	catch (var/exception/E)
		failed = TRUE
	ASSERT(failed)