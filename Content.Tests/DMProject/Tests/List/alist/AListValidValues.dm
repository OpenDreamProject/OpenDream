/proc/RunTest()
	var/alist/AL = alist()
	var/datum/D = new()
	var/list/L = new()
	AL[1] = D
	AL[2] = L
	AL[3] = 3
	AL[4] = "4"
	AL[5] = null
	ASSERT(AL[1] == D)
	ASSERT(AL[2] == L)
	ASSERT(AL[3] == 3)
	ASSERT(AL[4] == "4")
	ASSERT(AL[5] == null)