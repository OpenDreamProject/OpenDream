#include "Shared/operator_testing.dm"

/proc/power(a, b)
	return a ** b

/proc/RunTest()
	var/list/expected = list(
		10000000000,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
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
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
		1,
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
	
	test_binary_operator(/proc/power, expected)