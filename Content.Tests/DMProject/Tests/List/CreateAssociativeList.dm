/proc/RunTest()
	var/a = "World"
	var/b = "Hello"
	var/c = "Goodbye"

	// "a" = 1
	// "Hello" = 2
	// "c" = 3
	var/list/L = list(a = 1, (b) = 2, c = 3)

	ASSERT(L["a"] == 1)
	ASSERT(L["b"] == null)
	ASSERT(L[b] == 2)
	ASSERT(L["c"] == 3)
	ASSERT(L[c] == null)
	ASSERT(L.len == 3)
