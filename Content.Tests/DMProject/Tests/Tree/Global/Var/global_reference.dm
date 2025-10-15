// COMPILE ERROR OD0011

/proc/RunTest()
	var/list/L = list()
	for(global in L) // PARITY BREAK: In BYOND this is actually a `Cannot modify null.Northeast` runtime. Compiletime error is probably fine for OD.
		return