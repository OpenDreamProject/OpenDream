// RETURN TRUE

/proc/RunTest()
	var/foo = 5
	switch(foo)
		if(2)
			return FALSE
		else:
			return TRUE
	return FALSE
