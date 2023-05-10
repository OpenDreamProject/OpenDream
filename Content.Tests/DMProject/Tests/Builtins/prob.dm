
/proc/RunTest()
	rand_seed(0)
	var/n = 100000
	
	var/p = 0
	var/count = 0
	for(var/i in 1 to n)
		if(prob(p))
			count++
	ASSERT(count == 0)

	p = 0.1
	count = 0
	for(var/i in 1 to n)
		if(prob(p))
			count++
	ASSERT(count > 0 && count < n / 100 / 2)

	p = 100
	count = 0
	for(var/i in 1 to n)
		if(prob(p))
			count++
	ASSERT(count == n)
