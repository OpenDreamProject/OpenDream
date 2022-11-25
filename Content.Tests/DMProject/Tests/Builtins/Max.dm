/proc/RunTest()
	// Single arg returns that arg
	ASSERT(max(1) == 1)

	// Multiple numbers returns the largest number, no matter order
	ASSERT(max(1, 2, 3, 4) == 4)
	ASSERT(max(4, 3, 2, 1) == 4)
	ASSERT(max(1, 3, 4, 2) == 4)

	// Strings compare alphabetically
	ASSERT(max("a", "c", "b") == "c")

	// Various comparisons between null and other values
	ASSERT(max(null, null, null) == null)
	ASSERT(max(null, "") == "")
	ASSERT(max("", null) == "")
	ASSERT(max("", "str", null) == "str")
	ASSERT(max("", "str", "", null) == "str")
	ASSERT(max(5, null) == 5)
	ASSERT(max(-3, null) == null) // null > -3

	// null and 0 are equal here so the last one is returned
	ASSERT(max(0, null) == null)
	ASSERT(max(null, 0) == 0)
