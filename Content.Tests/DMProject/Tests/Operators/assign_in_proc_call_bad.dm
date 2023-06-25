// RUNTIME ERROR
//# issue 1225

/proc/foo(a)
	return a

/proc/RunTest()
	var/b = 0
	foo(b = 5)
