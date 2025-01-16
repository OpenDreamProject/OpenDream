
//# issue 635
  
var/A = 6

/proc/RunTest()
	ASSERT(initial(A) == 6)
	A = 8
	ASSERT(initial(A) == 8) // Somewhat surprisingly, initial() on a global returns the current value
