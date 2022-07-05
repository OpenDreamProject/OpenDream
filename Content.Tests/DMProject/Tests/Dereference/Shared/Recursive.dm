// IGNORE

/datum/recursive
	var/datum/recursive/inner
	var/val = 2

	proc/get_inner()
		. = inner