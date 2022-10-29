/proc/RunTest()
	ASSERT(md5("md5_test") == "c74318b61a3024520c466f828c043c79")
	ASSERT(md5(file('turf.dmi')) == "0ce21a42eed3205f5e22eabac6f02eaa")