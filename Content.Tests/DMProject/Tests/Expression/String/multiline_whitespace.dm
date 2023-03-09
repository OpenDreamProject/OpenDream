//# issue 23

var/str = "A\
		
	B"

/proc/RunTest()
	ASSERT(str == "AB")