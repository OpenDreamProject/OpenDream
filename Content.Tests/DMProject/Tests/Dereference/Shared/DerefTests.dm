// IGNORE

/datum/DerefTest1
	inner
		var/ele2 = 4

	var/ele = 2
	var/innerobj

	var/datum/DerefTest1/inner/innerobj_ty

	proc/inner_proc()
		return new /datum/DerefTest1

	proc/elefn()
		return 3
