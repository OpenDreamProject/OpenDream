// COMPILE ERROR OD0100
/proc/RunTest()
	var/list/L = list(1,2,3)
	nameof(L[2]) // nameof() isn't valid on a list index
