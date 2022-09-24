
//# issue 685

/obj/o
	var/A = 5
	var/B = 7

/proc/RunTest()
	var/obj/o/test = new
	test.A = 2
	test.B = 3
	ASSERT(initial(test.vars["A"]) == 5) // Note that this doesn't work on most lists and will instead return the current value
	ASSERT(initial(test.vars["B"]) == 7)
