
/datum/var/foo = 5
/proc/RunTest()
	var/datum/D = new
	D.foo = 6
	ASSERT(initial(D.foo) == 5)
