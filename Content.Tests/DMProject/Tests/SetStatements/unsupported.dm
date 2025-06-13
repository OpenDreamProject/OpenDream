// COMPILE ERROR OD2801

#pragma UnsupportedAccess error

/proc/A()
	set opendream_unsupported = "Wowza, no support"

/proc/RunTest()
	A()
