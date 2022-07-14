/datum/parent
	proc/f(a)
		return a

/datum/parent/child
	f(a)
		return ..()

/proc/RunTest()
	var/datum/parent/child/C = new()
	ASSERT(C.f(127) == 127)