
//# issue 384

/proc/RunTest()
	var/l1 = list(1,2,3)
	var/l2 = list(1,2,3,4)
	var/l3 = list(1,2)
	var/l4 = list(1,2,3,4)
	ASSERT((l1 ~= l1) == TRUE)
	ASSERT((l1 ~= l2) == FALSE)
	ASSERT((l1 ~= l3) == FALSE)
	ASSERT((l1 ~= l3) == FALSE)
	ASSERT((l1 ~! l1) == FALSE)
	ASSERT((l1 ~! l2) == TRUE)
	ASSERT((l1 ~! l3) == TRUE)
	ASSERT((l1 ~! l4) == TRUE)
	
	// As of BYOND 516, we now care about assoc values for equivalence
	var/list/one = list(a=1,b=3,c="hi")
	var/list/two = list(a=1,b=2,c="hi")
	var/list/three = list(a=1,b=3,c="hi")
	ASSERT((one ~! two) == TRUE)
	ASSERT((one ~= three) == TRUE)

	var/matrix/m1 = matrix(1,2,3,4,5,6)
	var/matrix/m2 = matrix(-1,-2,-3,-4,-5,6)
	var/matrix/m3 = matrix(1,2,3,4,5,6)
	m3.a = "poop"
	ASSERT((m1 ~= m1) == TRUE)
	ASSERT((m1 ~= m2) == FALSE)
	ASSERT((m1 ~= m3) == FALSE)
	ASSERT((m2 ~= m1) == FALSE)
	ASSERT((m2 ~= m2) == TRUE)
	ASSERT((m2 ~= m3) == FALSE)
	ASSERT((m3 ~= m1) == FALSE)
	ASSERT((m3 ~= m2) == FALSE)
	ASSERT((m3 ~= m3) == TRUE)

	var/matrix/m4 = matrix(1,2,3,4,5,6)
	ASSERT((m1 ~= m4) == TRUE)
	ASSERT((m2 ~= m4) == FALSE)
	ASSERT((m3 ~= m4) == FALSE)
	ASSERT((m4 ~= m4) == TRUE)

	ASSERT(("apple" ~= m1) == FALSE)
	ASSERT(("apple" ~! m1))
	ASSERT(m1 ~! "apple")
	ASSERT(m1 ~! m2)
	ASSERT(1 ~= 1)
