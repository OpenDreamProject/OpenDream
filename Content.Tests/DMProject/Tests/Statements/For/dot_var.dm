
//# issue 656

/proc/RunTest()
	var/list/L = list(1,2,3)
	var/a = 0
	for(. in L) 
		a += .
	ASSERT(a == 6)
