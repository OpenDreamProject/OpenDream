// COMPILE ERROR OD1000

// issue # 2283

#pragma FileAlreadyIncluded error

#include "test_module/test_file_to_include.dm"
#include "./test_module/test_file_to_include.dm"

/proc/RunTest()
	return
