/proc/RunTest()
	var/alist/AL = alist("a" = 1, "b" = 2, "c" = -4)
	var/alist/AL2 = AL | "A"
	ASSERT(AL2["c"] == -4)
	ASSERT(AL2["d"] == null)
	ASSERT("A" in AL2)
	ASSERT(length(AL2) == 4)

	AL2 = AL | list("c", "e")
	ASSERT(AL2["c"] == -4)
	ASSERT("e" in AL2)
	ASSERT(length(AL2) == 4)
	
	AL2 = AL | alist("c" = 6, "p" = 3)
	ASSERT(AL2["c"] == -4)
	ASSERT(AL2["p"] == 3)
	ASSERT(length(AL2) == 4)
