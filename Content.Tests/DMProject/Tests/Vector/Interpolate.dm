/proc/RunTest()
	var/vector/A = vector(1, 1)
	var/vector/B = vector(12, 124, 91)

	var/vector/result = A.Interpolate(B, 0)
	ASSERT(result.x == 1)
	ASSERT(result.y == 1)
	ASSERT(result.z == 0)
	ASSERT(result.len == 3)

	result = A.Interpolate(B, 1)
	ASSERT(result.x == 12)
	ASSERT(result.y == 124)
	ASSERT(result.z == 91)


	result = A.Interpolate(B, 3)
	ASSERT(result.x == 34)
	ASSERT(result.y == 370)
	ASSERT(result.z == 273)

	result = A.Interpolate(B, 0.5)
	ASSERT(result.x == 6.5)
	ASSERT(result.y == 62.5)
	ASSERT(result.z == 45.5)
