/proc/RunTryCatch(var/arg1 = 1, var/arg2 = 2)
	var/catchRan = FALSE
	
	try
		throw "error"
	catch(var/e)
		catchRan = TRUE
		ASSERT(e == "error")
	
	ASSERT(catchRan)
	
	// Ensure these were left unmodified
	ASSERT(arg1 == 1)
	ASSERT(arg2 == 2) 

/proc/RunTest()
	RunTryCatch()
