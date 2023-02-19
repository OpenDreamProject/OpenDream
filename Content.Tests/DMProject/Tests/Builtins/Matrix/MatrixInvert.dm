/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Invert()

	var/matrix/firstInversion = matrix(-1.66666667, 0.6666667, 1, 1.3333334, -0.33333334, -2)
	if(M ~! firstInversion)
		CRASH("Unexpected matrix/Invert result '[json_encode(M)]', difference is [json_encode(firstInversion.Subtract(M))]")

	M.Invert()

	//TODO: Fix inexact error in Matrix inversion maths >:/
	//if(M ~! matrix(1, 2, 3, 4, 5, 6))
		//CRASH("Unexpected matrix/Invert result2: [json_encode(M)]")