// COMPILE ERROR OD0011

/proc/RunTest()
	var/list/l1 = list(1,2,3)
	var/list/l2 = list(4,5,6)
	var/out = 0
	for (var/i in l1 in l2)
		out += i
