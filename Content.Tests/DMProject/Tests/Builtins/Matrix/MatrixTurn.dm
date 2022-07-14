﻿#include "Shared/MatrixEquals.dm"

/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Turn(90)

	if(!M.Equals(matrix(4, 5, 6, -1, -2, -3)))
		CRASH("Unexpected matrix/Turn result")