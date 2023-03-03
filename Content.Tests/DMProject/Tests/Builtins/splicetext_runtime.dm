/proc/RunTest()
	var/passed = TRUE
	try
		splicetext("banana", 12, -1, "test") //bad text or out of bounds
		passed = FALSE
	catch
	ASSERT(passed && "test1")

	try
		splicetext("banana", 3, -5, "test") //bad text or out of bounds
		passed = FALSE
	catch

	ASSERT(passed && "test2")
	try
		splicetext("banana", 0, 6, "laclav") //bad text or out of bounds
		passed = FALSE
	catch
	ASSERT(passed && "test3")

	try
		splicetext("abcdef", 4, 3, "test") //bad text or out of bounds
		passed = FALSE
	catch
	ASSERT(passed && "test4")
