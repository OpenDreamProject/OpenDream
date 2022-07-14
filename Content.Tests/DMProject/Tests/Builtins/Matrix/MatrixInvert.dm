// IGNORE
// Invert currently fails because OpenDream doesn't handle floats the way BYOND does.
// Un-ignore when that's changed.
#include "Shared/MatrixEquals.dm"

/proc/RunTest()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Invert()

	if(!M.Equals(matrix(-1.6666667, 0.6666667, 1, 1.3333334, -0.33333334, -2)))
		CRASH("Unexpected matrix/Invert result [M.a] [M.b] [M.c] [M.d] [M.e] [M.f]")

	M.Invert()

	if(!M.Equals(matrix(1, 2, 3, 4, 5, 6)))
		CRASH("Unexpected matrix/Invert result2 [M.a] [M.b] [M.c] [M.d] [M.e] [M.f]")