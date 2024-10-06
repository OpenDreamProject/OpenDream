// COMPILE ERROR OD2101
#pragma DuplicateProcDefinition error

//# issue 172

/proc/a()
	return 1

/proc/a()
	return 2

/proc/RunTest()
	a()
