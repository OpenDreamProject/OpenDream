
/proc/RunTest()
	if(1).
		ASSERT(TRUE)
	else
		ASSERT(FALSE)
	if(2):
		ASSERT(TRUE)
	else
		ASSERT(FALSE)
	for(var/i in 1 to 1):
		ASSERT(TRUE)
	for(var/i in 1 to 1).
		ASSERT(TRUE)
