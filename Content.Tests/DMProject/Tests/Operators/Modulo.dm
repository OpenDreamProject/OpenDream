#include "Shared/operator_testing.dm"

/proc/modulo(a, b)
	return a % b

/proc/RunTest()
	var/list/expected = list(
		0,
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		0,
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error",
		"Error"
	)
	
	test_binary_operator(/proc/modulo, expected)
	
	// Some more for good measure:
	
	// simple % cases
	ASSERT(10 % 2 == 0)
	ASSERT(32 % 6 == 2)
	ASSERT(5 % 256 == 5)
	ASSERT(null % 10 == 0) // coerce null into 0 for left side

	// above cases but for %=
	var/A = 10
	A %= 2
	ASSERT(A == 0)

	var/B = 32
	B %= 6
	ASSERT(B == 2)

	var/C = 5
	C %= 256
	ASSERT(C == 5)

	var/D = null
	D %= 10
	ASSERT(D == 0)