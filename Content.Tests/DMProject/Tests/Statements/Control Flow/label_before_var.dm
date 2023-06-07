
//# issue 1032

#define ASSERT(x) x

/proc/RunTest()
	label:
	var/b = 5
	ASSERT(b == 5)
