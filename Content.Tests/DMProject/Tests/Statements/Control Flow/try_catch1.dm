
//# issue 694

/proc/RunTest()
	var/a
	try   
		a = 1
	catch(var/e)   
		a = 2
	ASSERT(a == 1)
