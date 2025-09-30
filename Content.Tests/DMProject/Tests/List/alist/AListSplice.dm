// RUNTIME ERROR

/proc/RunTest()
	var/alist/AL = alist("a" = 1, "b" = -2, "c" = 5.05)
	AL.Splice(1, 1, "d")