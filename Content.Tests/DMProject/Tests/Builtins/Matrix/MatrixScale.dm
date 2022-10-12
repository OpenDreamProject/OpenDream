
/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Scale(2)

	if(M ~! matrix(2, 4, 6, 8, 10, 12))
		CRASH("Unexpected matrix/Scale result: [json_encode(M)]")