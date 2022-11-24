
//# issue 690

/proc/RunTest()
	var/list/A = list(1,2,3)
	var/list/B = list(3,4,5,6)
	ASSERT((A & B).len == 1)
	ASSERT((A | B).len == 6) 
