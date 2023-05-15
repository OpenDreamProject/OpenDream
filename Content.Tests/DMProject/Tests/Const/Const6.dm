/proc/RunTest()
	var/a = 137
	switch(a)
		if(20)
			. = 500
		if(136 | 1)
			. = 1
		if(/datum, /mob)
			. = 300
	ASSERT(. == 1)