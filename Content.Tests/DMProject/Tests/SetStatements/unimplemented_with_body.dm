// COMPILE ERROR OD2800

#pragma UnimplementedAccess error

/proc/A()
	set opendream_unimplemented = TRUE
	return

/proc/RunTest()
	A()
