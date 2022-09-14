
//# issue 395

/proc/RunTest()
	var/pipe = 2
	switch(pipe)
		if(1,2,3,)
			return

	ASSERT(FALSE)
