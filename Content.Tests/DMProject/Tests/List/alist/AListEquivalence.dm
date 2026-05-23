/proc/RunTest()
	var/datum/D = new
	var/alist/AL = alist("a" = null, "b" = null, "c" = null)
	var/alist/AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	var/list/L = list("a", "b", "c")
	var/list/L2 = list("a" = 1, "b" = 2, "c" = 3)
	
	ASSERT(AL ~= AL)
	ASSERT(!(AL ~= AL2))
	ASSERT(!(AL ~= D))
	ASSERT(AL ~= L)
	ASSERT(!(AL ~= L2))