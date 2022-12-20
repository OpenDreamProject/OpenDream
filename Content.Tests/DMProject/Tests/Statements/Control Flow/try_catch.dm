
//# issue 694

/proc/RunTest()
	var/a
	try
		a = 1
	catch(var/e)
		a = 2
	ASSERT(a == 1)

	try
		throw "test"
	catch

	try
		throw "test"
	catch(var/e2)

	try
		throw "test"
	catch(var/e3)
		ASSERT(e3 == "test")
