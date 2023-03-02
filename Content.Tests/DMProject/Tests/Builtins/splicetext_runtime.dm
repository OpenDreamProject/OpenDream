/proc/RunTest()
	try
		splicetext("banana", 12, -1, "test") //bad text or out of bounds
		ASSERT("THIS TEST SHOULD NOT PASS" == 0)
	catch
		ASSERT(TRUE)
	try
		splicetext("banana", 3, -5, "test") //bad text or out of bounds
		ASSERT("THIS TEST SHOULD NOT PASS" == 0)
	catch
		ASSERT(TRUE)
	try
		splicetext("banana", 0, 6, "laclav") //bad text or out of bounds
		ASSERT("THIS TEST SHOULD NOT PASS" == 0)
	catch
		ASSERT(TRUE)
	try
		splicetext("abcdef", 4, 3, "test") //bad text or out of bounds
		ASSERT("THIS TEST SHOULD NOT PASS" == 0)
	catch
		ASSERT(TRUE)						
