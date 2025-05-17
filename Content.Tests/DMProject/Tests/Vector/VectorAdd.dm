/proc/RunTest()
	var/vector/A = vector(3, 3)
	var/vector/B = vector(4, 4, 4)

	var/vector/result = A + B
	ASSERT(result.x == 7)
	ASSERT(result.y == 7)
	ASSERT(result.z == 4)

	A += B
	ASSERT(A.x == 7)
	ASSERT(A.y == 7)
	ASSERT(A.z == 4)
