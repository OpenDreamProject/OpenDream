
/datum/o
	var/A
	var/tmp/B

/datum/o/proc/IsSavedSrcVars()
	ASSERT(issaved(A))
	ASSERT(!issaved(B))
	ASSERT(issaved(vars["A"]))
	ASSERT(!issaved(vars["B"]))

/proc/RunTest()
	var/datum/o/test = new
	ASSERT(!issaved(test.type))
	ASSERT(issaved(test.A))
	ASSERT(!issaved(test.B))

	// Note that this doesn't work on most lists and will instead return false
	ASSERT(issaved(test.vars["A"]))
	ASSERT(!issaved(test.vars["B"]))

	/*
	var/expected = prob(50)
	var/random_key = expected ? "A" : "B"
	ASSERT(issaved(test.vars[random_key]) == expected)
	*/

	test.IsSavedSrcVars()
