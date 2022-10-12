
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Add(N)

	if(M ~! matrix(8, 10, 12, 14, 16, 18))
		CRASH("Unexpected matrix/Add result: [json_encode(M)]")