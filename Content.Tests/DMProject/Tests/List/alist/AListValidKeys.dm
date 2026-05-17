/proc/RunTest()
	var/alist/AL = alist()
	var/datum/D = new()
	var/list/L = new()
	AL[D] = 1
	AL["A"] = 2
	AL[5] = 3
	AL[L] = 4
	AL[null] = 5
	ASSERT(AL[D] == 1)
	ASSERT(AL["A"] == 2)
	ASSERT(AL[5] == 3)
	ASSERT(AL[L] == 4)
	ASSERT(AL[null] == 5)