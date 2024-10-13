// COMPILE ERROR OD2207
#pragma SuspiciousMatrixCall error

/proc/RunTest()
	var/matrix/M = matrix("poop","butt","poopbutt"); // Hey... that don't look right!