
//# issue 436

/proc/RunTest()
	var/a
	try
		throw 5
	catch(var/e)
		a = e
	ASSERT(a == 5)
