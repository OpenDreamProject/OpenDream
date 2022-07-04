// RUNTIME ERROR, RETURN TRUE

/proc/RunTest()
	. = TRUE
	while(1)
		RunTest()
	. = FALSE