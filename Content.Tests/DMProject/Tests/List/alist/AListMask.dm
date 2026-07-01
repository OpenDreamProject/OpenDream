/proc/RunTest()
	var/alist/AL = alist("a" = null, "b" = null, "c" = null)
	var/alist/AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	var/alist/AL3 = alist("a" = 1, "b" = 2, "c" = 3)
	var/list/L = list("a", "b", "c")
	var/list/L2 = list("a", "b", "d")
	var/list/L3 = list("a" = 1, "b" = 2, "c" = 3)
	var/list/L4 = list("a" = 2, "e" = 4)
	
	AL &= "a"
	ASSERT("a" in AL)
	ASSERT(!("b" in AL))
	ASSERT(!("c" in AL))
	ASSERT(length(AL) == 1)
	
	AL &= "b"
	ASSERT(!("a" in AL))
	ASSERT(!("b" in AL))
	ASSERT(!("c" in AL))
	ASSERT(length(AL) == 0)
	
	AL2 &= AL2
	ASSERT(AL2["a"] == 1)
	ASSERT(AL2["b"] == 2)
	ASSERT(AL2["c"] == 3)
	ASSERT(length(AL2) == 3)
	
	AL2 &= AL3
	ASSERT(AL2["a"] == 1)
	ASSERT(AL2["b"] == 2)
	ASSERT(AL2["c"] == 3)
	ASSERT(length(AL2) == 3)
	
	AL2 &= L
	ASSERT(AL2["a"] == 1)
	ASSERT(AL2["b"] == 2)
	ASSERT(AL2["c"] == 3)
	ASSERT(length(AL2) == 3)
	
	AL2 &= L2
	ASSERT(AL2["a"] == 1)
	ASSERT(AL2["b"] == 2)
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 2)
	
	AL2 &= L3
	ASSERT(AL2["a"] == 1)
	ASSERT(AL2["b"] == 2)
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 2)
	
	AL2 &= L4
	ASSERT(AL2["a"] == 1)
	ASSERT(!("b" in AL2))
	ASSERT(!("c" in AL2))
	ASSERT(length(AL2) == 1)