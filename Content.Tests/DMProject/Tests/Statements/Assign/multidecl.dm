
//# issue 18

/proc/RunTest()
	var/list/l = list(1,2,3)
	var/i, ch, len = length(l)
	ASSERT(i == null)
	ASSERT(ch == null)
	ASSERT(len == 3)
