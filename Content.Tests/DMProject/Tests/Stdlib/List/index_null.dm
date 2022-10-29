
//# issue 84

/proc/RunTest()
	var/l = list(1,2,3)
	l[null] = 5
	ASSERT(l[null] == 5)
