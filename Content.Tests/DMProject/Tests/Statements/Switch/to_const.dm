
//# issue 699

/proc/RunTest()
	var/test = 5
	var/const/test_const = 2	
	switch(test)
		if(test_const*0.5 to test_const)
			ASSERT(FALSE)
		if(1 to test_const*0.5)
			ASSERT(FALSE)
			