
//# issue 154 

/proc/RunTest()
	var/list/out = list()
	var/list/L = list(list(1, 2), list(3, 4))
	for(var/item1 in L)
		for (var/item2 in item1)
			out += item2
			goto label
		label:
	
	ASSERT(out.len == 2)
	ASSERT(out[1] == 1)
	ASSERT(out[2] == 3)
