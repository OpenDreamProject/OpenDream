/proc/RunTest()
	var/counter = 0
	for(var/i in 1 to 3)
		counter++
	ASSERT(counter == 3)

	counter = 0
	for(var/i = 1 to 3)
		counter++
	ASSERT(counter == 3)

	counter = 0
	var/j = 1
	for(,j <= 3,j++)
		counter++
	ASSERT(counter == 3)

	counter = 0
	j = 1
	for(,j++ <= 3)
		counter++
	ASSERT(counter == 3)