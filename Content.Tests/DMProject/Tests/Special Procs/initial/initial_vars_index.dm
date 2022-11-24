
//# issue 685

/obj/o
	var/A = 5
	var/B = 7

/obj/o/proc/InitialSrcVars()
	ASSERT(initial(vars["A"]) == 5)

/proc/RunTest()
	var/obj/o/test = new
	test.A = 2
	test.B = 3
	ASSERT(initial(test.vars["A"]) == 5) // Note that this doesn't work on most lists and will instead return the current value
	ASSERT(initial(test.vars["B"]) == 7)

	var/random_key = prob(50) ? "A" : "B"
	var/expected = random_key == "A" ? 5 : 7
	ASSERT(initial(test.vars[random_key]) == expected)

	test.InitialSrcVars()
