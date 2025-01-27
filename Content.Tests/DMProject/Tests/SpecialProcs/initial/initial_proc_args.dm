
/proc/someargs(a, b = 2, c = "c") // initial() on args returns their current value
	ASSERT(initial(a) == "3")
	ASSERT(initial(b) == 5)
	ASSERT(initial(c) == "cc")

	b = 5
	c = "e"

	ASSERT(initial(b) == 5)
	ASSERT(initial(c) == "e")

/proc/RunTest()
	someargs("3", 5, c = "cc") 
