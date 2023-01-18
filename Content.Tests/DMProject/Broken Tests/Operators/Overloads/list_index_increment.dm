/datum/fuck
	var/x=1
	var/y=2

	New(x,y)
		src.x = x
		src.y = y

	proc/operator[](var/index)
		if(index == 1)
			return src.x
		else
			return src.y

	proc/operator[]=(var/index, var/value)
		if(index == 1)
			src.x = value
		else
			src.y = value

/proc/RunTest()
	var/list/A = list()
	A["key"] = 0
	A["key"]++
	ASSERT(A["key"] == 1)
	var/datum/fuck/G = new(3,10)
	G[1] = 5
	G[1]++
	ASSERT(G.x == 6)