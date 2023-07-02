
/datum/test/var/foo = "foo" 
/datum/test2/var/bar = "bar" 

/proc/RunTest() 
	var/type = /datum/test 
	ASSERT((new type()).foo == "foo")

	type = /datum/test2
	var/ex = null
	try
		(new type()).foo
	catch (var/exception/e)
		ex = e
	ASSERT(ex != null)
