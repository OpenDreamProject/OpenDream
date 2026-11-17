// COMPILE ERROR OD2102

//# issue 2648

/datum/proc/beep()
	return

// "beep" is already a type here, so BYOND reads this as a duplicate definition rather than an override
/datum/test/beep
	var/thing = 1

/datum/test/beep()
	return

/proc/RunTest()
	return
