/proc/RunTest()
	var/counter = 0
	for(var/i in 1 to 3)
		counter++
	ASSERT(counter == 3)

	counter = 0
	for(var/i = 1 to 3)
		counter++
	ASSERT(counter == 3)

	var/i = -1
	for(i in 1 to 0) // An end bound lower than the start bound should not assign to i
		continue
	ASSERT(i == -1)

	i = -1
	for(i in list()) // This should though!
		continue
	ASSERT(i == null)

	i = -1
	for(i in list(1)) // Even ends up being null if the list isn't empty
		continue
	ASSERT(i == null)

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
