// RUNTIME ERROR, NO RETURN

/proc/RunTest()
	. = TRUE
	while(1)
		RunTest()
	. = FALSE