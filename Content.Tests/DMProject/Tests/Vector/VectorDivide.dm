/proc/RunTest()
	var/vector/A = vector(3, 3)
	var/vector/B = vector(4, 4, 4)
	var/vector/result = B / 2
	ASSERT(result.x == 2)
	ASSERT(result.y == 2)
	ASSERT(result.z == 2)

	result = A / 2
	ASSERT(result.x == 1.5)
	ASSERT(result.y == 1.5)
	ASSERT(result.z == 0)

	result = A / B
	ASSERT(result.x == 0.75)
	ASSERT(result.y == 0.75)
	ASSERT(result.z == 0)

	A /= B
	ASSERT(A.x == 0.75)
	ASSERT(A.y == 0.75)
	ASSERT(A.z == 0)