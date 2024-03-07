// Just testing that this parses
/proc/foo() as /list
	return list("a","b","c")

/proc/RunTest()
	ASSERT(foo()[2] == "b")
