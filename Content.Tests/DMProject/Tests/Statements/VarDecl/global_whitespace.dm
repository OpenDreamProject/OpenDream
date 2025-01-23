
//# issue 2139

var/foo = 2
var bar = 3

/proc/RunTest()
	ASSERT(foo == 2)
	ASSERT(bar == 3)
