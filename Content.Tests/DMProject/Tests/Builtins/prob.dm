
/proc/RunTest()
	rand_seed(0)
	var/n = 100000

	var/count = 0
	for(var/i in 1 to n)
		if(prob(0))
			count++
	ASSERT(count == 0)

	count = 0
	for(var/i in 1 to n)
		if(prob(0.1))
			count++
	ASSERT(count > 0 && count < n / 100)

	count = 0
	for(var/i in 1 to n)
		if(prob(100))
			count++
	ASSERT(count == n)