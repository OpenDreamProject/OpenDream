// RUNTIME ERROR

var/ConstZero1_a

/proc/RunTest()
	ConstZero1_a = 1 / RunTest()
	return 1
