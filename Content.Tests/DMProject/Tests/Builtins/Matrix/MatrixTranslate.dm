#include "Shared/MatrixEquals.dm"

/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Translate(2)

	if(!M.Equals(matrix(1, 2, 5, 4, 5, 8)))
		CRASH("Unexpected matrix/Translate result")