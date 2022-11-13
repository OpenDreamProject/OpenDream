
/proc/RunTest()
	var/const/c = 6
	switch (1)
		if (c)
			ASSERT(0)
		else
			ASSERT(1)
