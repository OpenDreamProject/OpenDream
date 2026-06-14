/proc/RunTest()
	var/alist/AL = alist("a" = 1, "b" = 2, "c" = -4)
	AL |= "A"
	ASSERT(AL["c"] == -4)
	ASSERT(AL["d"] == null)
	ASSERT("A" in AL)
	ASSERT(length(AL) == 4)

	AL |= list("c", "e")
	ASSERT(AL["c"] == -4)
	ASSERT("e" in AL)
	ASSERT(length(AL) == 5)
	
	AL |= alist("c" = 6, "p" = 3)
	ASSERT(AL["c"] == -4)
	ASSERT(AL["p"] == 3)
	ASSERT(length(AL) == 6)
