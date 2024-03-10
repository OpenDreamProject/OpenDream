/datum/extraException
	var/test = "test"

/proc/RunTest()
	try 
		throw EXCEPTION(new /datum/extraException())
	catch(var/exception/e)
		var/datum/extraException/dat = e.name
		ASSERT(dat.test == "test")
