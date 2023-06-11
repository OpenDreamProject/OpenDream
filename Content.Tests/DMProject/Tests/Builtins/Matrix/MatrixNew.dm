/proc/RunTest()
	var/matrix/M = matrix(1,2,3,4,5,6)
	var/matrix/N = matrix()
	var/matrix/B = matrix(M)

	if(B ~! M)
		CRASH("Unexpected matrix/New/Copy result: [json_encode(M)] & [json_encode(B)]")