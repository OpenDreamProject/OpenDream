/proc/RunTest()
	var/holder
	var/alist/AL = alist("a" = null, "b" = null, "c" = null)
	var/alist/AL2 = alist("a" = 1, "b" = 2, "c" = 3)
	var/alist/AL3 = alist("a" = 1, "b" = 2, "c" = 3)
	var/list/L = list("a", "b", "c")
	var/list/L2 = list("a", "b", "e")
	var/list/L3 = list("a" = 1, "b" = 2, "c" = 3)
	var/list/L4 = list("a" = 2, "e" = 4)
	
	holder = AL - "a"
	ASSERT(!("a" in holder))
	ASSERT(length(holder) == 2)
	
	holder = holder - "d"
	ASSERT(!("a" in holder))
	ASSERT("b" in holder)
	ASSERT("c" in holder)
	ASSERT(length(holder) == 2)
	
	holder = AL2 - AL2
	ASSERT(!("a" in holder))
	ASSERT(!("b" in holder))
	ASSERT(!("c" in holder))
	ASSERT(length(holder) == 0)
	
	holder = alist("a" = 1, "b" = 2, "c" = 3) - AL3
	ASSERT(!("a" in holder))
	ASSERT(!("b" in holder))
	ASSERT(!("c" in holder))
	ASSERT(length(holder) == 0)
	
	holder = alist("a" = 1, "b" = 2, "c" = 3) - L
	ASSERT(!("a" in holder))
	ASSERT(!("b" in holder))
	ASSERT(!("c" in holder))
	ASSERT(length(holder) == 0)
	
	holder = alist("a" = 1, "b" = 2, "c" = 3) - L2
	ASSERT(!("a" in holder))
	ASSERT(!("b" in holder))
	ASSERT("c" in holder)
	ASSERT(length(holder) ==  1)
	
	holder = holder - L3
	ASSERT(!("a" in holder))
	ASSERT(!("b" in holder))
	ASSERT(!("c" in holder))
	ASSERT(length(holder) == 0)
	
	holder = alist("a" = 1, "b" = 2, "c" = 3) - L4
	ASSERT(!("a" in holder))
	ASSERT("b" in holder)
	ASSERT("c" in holder)
	ASSERT(length(holder) == 2)