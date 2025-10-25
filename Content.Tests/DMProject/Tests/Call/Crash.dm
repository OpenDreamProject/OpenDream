// RUNTIME ERROR, RETURN TRUE

/proc/RunTest()
	. = TRUE
	CRASH("This should stop the current proc")
	. = FALSE