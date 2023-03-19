
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Multiply(N)

	if(M ~! matrix(39, 54, 78, 54, 75, 108))
		CRASH("Unexpected matrix/Multiply result: [json_encode(M)]")
