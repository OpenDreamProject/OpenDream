#include "test_module/test_file_to_include.dm"

/proc/RunTest()
	ASSERT(test_var == "test_var_value")
