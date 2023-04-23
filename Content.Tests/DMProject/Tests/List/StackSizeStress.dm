/proc/RunTest()
	var/list/a = list("a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p")
	var/list/b = list()
	for(var/key in a)
		if(!b[key])
			b[key] = list()
		for(var/i in 1 to 10)
			b[key] += i

	ASSERT(b["p"][5] == 5)