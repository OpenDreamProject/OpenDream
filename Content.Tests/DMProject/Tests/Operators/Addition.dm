#include "Shared/operator_testing.dm"

/proc/add(a, b)
	return a + b

/proc/RunTest()
	var/list/result = test_operator(/proc/add)
	
	CRASH(json_encode(result))