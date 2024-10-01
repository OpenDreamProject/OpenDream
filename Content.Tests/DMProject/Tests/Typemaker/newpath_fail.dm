// COMPILE ERROR OD2701
#pragma InvalidVarType error
/proc/foo(turf/bar as turf)
	bar = new /obj(null)

/proc/RunTest()
	return