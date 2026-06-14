/proc/RunTest()
	try
		try
			throw 5
		catch(var/e)
			ASSERT(e == 5)
			throw 10
	catch(var/e)
		ASSERT(e == 10)
