/proc/RunTest()
	var/a = 10
	
	var/result = (a -= 8)
	ASSERT(result == 2)
	ASSERT(a == 2)
	
	a = null
	result = (a -= null)
	ASSERT(result == 0)
	ASSERT(a == 0)
	
	a = null
	result = (a -= 5)
	ASSERT(result == -5)
	ASSERT(a == -5)