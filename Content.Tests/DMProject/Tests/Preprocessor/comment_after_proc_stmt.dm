
//# issue 31

/proc/RunTest()
	var/l = list(4,5,6)
	for(var/A in l)
		if(A + 2 > 5)
			continue //Here is a comment. This would cause an exception.

