
 //# issue 441

/proc/RunTest()
	var/x = 0
	var/sv = 548

	switch(sv)
		if(5 || x)
			ASSERT(FALSE)
		if(0 && x)
			ASSERT(FALSE)
		if("a" || x)
			ASSERT(FALSE)
		if("" && x)
			ASSERT(FALSE)
		if(5.5 || x)
			ASSERT(FALSE)
		if(0.0 && x)
			ASSERT(FALSE)
