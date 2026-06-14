
//# issue 679

/datum/step/ty
/datum/throw/ty
/datum/null/ty
/datum/switch/ty
/datum/spawn/ty

/proc/RunTest()
	var/datum/throw/o = new
	ASSERT(istype(o, /datum/throw))
