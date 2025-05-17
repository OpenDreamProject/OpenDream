/proc/RunTest()
	var/vector/A = vector(1, 1)
	var/vector/B = vector(12, 124, 91)

	ASSERT(A.Dot(B) == 136)
	ASSERT(B.Dot(A) == 136)
	ASSERT(A.Dot(A) == 2)
	ASSERT(B.Dot(B) == 23801)
