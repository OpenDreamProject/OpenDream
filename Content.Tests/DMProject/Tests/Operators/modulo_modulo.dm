// NO BYOND
// BREAKING CHANGE: The floating point precision is slightly different between OD and BYOND, giving slightly different values
/proc/RunTest()
	ASSERT(5.89254 %% 2.3462 == 1.2001398)
	var/A = 5.89254
	A %%= 2.3462
	ASSERT(A == 1.2001398)
