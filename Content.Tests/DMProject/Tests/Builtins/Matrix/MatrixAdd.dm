
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Add(N)

	if(M ~! matrix(8, 10, 12, 14, 16, 18))
		CRASH("Unexpected matrix/Add result: [json_encode(M)]")

	M = M + N

	if(M ~! matrix(15,18,21,24,27,30))
		CRASH("Unexpected matrix/OperatorAdd result: [json_encode(M)]")
		
	M += N

	if(M ~! matrix(22,26,30,34,38,42))
		CRASH("Unexpected matrix/OperatorAppend result: [json_encode(M)]")