/proc/RunTest()
	var/alist/AL = alist("a" = null, "b" = null, "c" = null)
	var/alist/AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	var/alist/AL3 = alist("a" = 1, "b" = 2, "c" = 3)
	var/list/L = list("a", "b", "c")
	var/list/L2 = list("a", "b", "e")
	var/list/L3 = list("a" = 1, "b" = 2, "c" = 3)
	var/list/L4 = list("a" = 2, "e" = 4)
	
	AL -= "a"
	ASSERT(!("a" in AL))
	ASSERT(length(AL) == 2)
	
	AL -= "d"
	ASSERT(!("a" in AL))
	ASSERT("b" in AL)
	ASSERT("c" in AL)
	ASSERT(length(AL) == 2)
	
	var/holder = AL2
	AL2 -= holder
	ASSERT(!("a" in AL2))
	ASSERT(!("b" in AL2))
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 0)
	
	AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	AL2 -= AL3
	ASSERT(!("a" in AL2))
	ASSERT(!("b" in AL2))
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 0)
	
	AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	AL2 -= L
	ASSERT(!("a" in AL2))
	ASSERT(!("b" in AL2))
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 0)
	
	AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	AL2 -= L2
	ASSERT(!("a" in AL2))
	ASSERT(!("b" in AL2))
	ASSERT("c" in AL2)
	ASSERT(length(AL2) ==  1)
	
	AL3 -= L3
	ASSERT(!("a" in AL3))
	ASSERT(!("b" in AL3))
	ASSERT(!("c" in AL3))
	ASSERT(length(AL3) == 0)
	
	AL3 = alist("a" = 1, "b" = 2, "c" = 3)
	AL3 -= L4
	ASSERT(!("a" in AL3))
	ASSERT("b" in AL3)
	ASSERT("c" in AL3)
	ASSERT(length(AL3) == 2)
