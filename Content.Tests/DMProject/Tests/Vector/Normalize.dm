/proc/RunTest()
	var/vector/A = vector(1, 1)
	var/vector/B = vector(12, 124, 91)
	A.Normalize()
	B.Normalize()

	ASSERT(A.x == sqrt(2)/2)
	ASSERT(A.y == sqrt(2)/2)
	ASSERT(A.size == 1)

	ASSERT(B.x == 12/sqrt(23801))
	ASSERT(B.y == 124/sqrt(23801))
	ASSERT(B.z == 91/sqrt(23801))
	ASSERT(B.size == 1)
