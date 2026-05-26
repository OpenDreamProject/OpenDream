/proc/RunTest()
	var/datum/D = new
	var/alist/AL = alist("a" = null, "b" = null, "c" = null)
	var/alist/AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	var/list/L = list("a", "b", "c")
	var/list/L2 = list("a" = 1, "b" = 2, "c" = 3)
	var/list/L3 = list("a", "b", "c", "d")
	var/list/L4 = list("a", "b", "d")
	
	
	ASSERT(AL ~= AL)
	ASSERT(!(AL ~= AL2))
	ASSERT(!(AL ~= D))
	ASSERT(AL ~= L)
	ASSERT(!(AL ~= L2))
	ASSERT(!(AL ~= L3))
	ASSERT(!(AL ~= L4))