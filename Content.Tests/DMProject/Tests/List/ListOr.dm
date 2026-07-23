/proc/RunTest()
	var/list/L1 = list("a" = 1, "b" = 2)
	var/list/L2 = list("c" = 3, "d" = 4)
	var/list/result = L1 | L2
	
	ASSERT(result ~= list("a" = 1, "b" = 2, "c" = 3, "d" = 4))