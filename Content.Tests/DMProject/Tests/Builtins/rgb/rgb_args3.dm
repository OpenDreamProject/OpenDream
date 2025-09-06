// COMPILE ERROR OD2208
#pragma FallbackBuiltinArgument error
/datum/foo/var/bar = rgb(1,2,null)
/proc/RunTest()
	return