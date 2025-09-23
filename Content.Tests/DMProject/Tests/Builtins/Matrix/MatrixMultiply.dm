
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Multiply(N)
	if(M ~! matrix(39, 54, 78, 54, 75, 108))
		CRASH("Unexpected matrix/Multiply result: [json_encode(M)]")

	M.Multiply(null) // Doesn't do anything
	if(M ~! matrix(39, 54, 78, 54, 75, 108))
		CRASH("Unexpected matrix/Multiply(null) result: [json_encode(M)]")

	M = matrix(1, 1, 1, 1, 1, 1)

	M.Multiply(2)
	if(M ~! matrix(2, 2, 2, 2, 2, 2))
		CRASH("Unexpected matrix/Multiply(2) result: [json_encode(M)]")

	M.Multiply("a")
	if(M ~! matrix(0, 0, 0, 0, 0, 0))
		CRASH("Unexpected matrix/Multiply(\"a\") result: [json_encode(M)]")

	M = matrix()
	M *= 2
	if(M ~! matrix(2, 0, 0, 0, 2, 0))
		CRASH("Unexpected matrix *= 2 result: [json_encode(M)]")

	M /= 2
	if(M ~! matrix(1, 0, 0, 0, 1, 0))
		CRASH("Unexpected matrix /= 2 result: [json_encode(M)]")

	N = new /matrix(M)
	ASSERT(M ~= N && M != N)