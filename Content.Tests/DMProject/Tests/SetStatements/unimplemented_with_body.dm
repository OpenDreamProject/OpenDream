// COMPILE ERROR

#pragma UnimplementedAccess error

/proc/A()
	set opendream_unimplemented = TRUE
	return

/proc/RunTest()
	A()
