/proc/RunTest()
	var/list/a = list("a")
	var/list/b = list("b")
	var/list/c = list("b", 1)
	
	// Parsed as "a" in (a || "b")
	ASSERT(("a" in a || "b") == 1)
	
	// Parsed as ("a" in (a || "b")) in b
	ASSERT(("a" in a || "b" in b) == 0)
	ASSERT(("a" in a || "b" in c) == 1)