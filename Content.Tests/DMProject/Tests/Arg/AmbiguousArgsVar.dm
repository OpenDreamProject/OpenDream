// NOBYOND
#pragma SoftReservedKeyword warning

//global src def
var/src = 321

/datum/proc/argstest(thing, args, beep)
	ASSERT(args == 2)

/datum/proc/srctest()
	ASSERT(istype(src, /datum)) // will fail if the global takes precedence over the built-in
	ASSERT(src != 321)

/proc/RunTest()
	var/datum/O = new()
	O.argstest(1, 2, 3)
	O.srctest()

