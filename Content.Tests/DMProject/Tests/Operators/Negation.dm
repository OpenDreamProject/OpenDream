#include "Shared/operator_testing.dm"

/proc/negate(a, b)
	return -a

/proc/RunTest()
	var/list/expected = list(
		-10,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0
	)
	
	test_unary_operator(/proc/negate, expected)