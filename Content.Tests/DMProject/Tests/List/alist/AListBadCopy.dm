// RUNTIME ERROR

/proc/RunTest()
	var/alist/AL = alist("a" = 1, "b" = 2, "c" = -4)
	var/alist/AL2
	AL2 = AL.Copy(1, 2)
