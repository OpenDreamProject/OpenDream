/proc/RunTest()
	var/vector/A = vector(3, 3)
	var/vector/B = vector(4, 4, 4)
	var/vector/result = B * 2
	ASSERT(result.x == 8)
	ASSERT(result.y == 8)
	ASSERT(result.z == 8)

	result = A * 2
	ASSERT(result.x == 6)
	ASSERT(result.y == 6)
	ASSERT(result.z == 0)

	result = A * B
	ASSERT(result.x == 12)
	ASSERT(result.y == 12)
	ASSERT(result.z == 0)

	A *= B
	ASSERT(A.x == 12)
	ASSERT(A.y == 12)
	ASSERT(A.z == 0)