#include "Shared/MatrixEquals.dm"

/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Subtract(N)

	if(!M.Equals(matrix(-6, -6, -6, -6, -6, -6)))
		CRASH("Unexpected matrix/Subtract result: [json_encode(M)]")