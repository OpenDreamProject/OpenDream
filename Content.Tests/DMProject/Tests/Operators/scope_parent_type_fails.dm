// COMPILE ERROR OD0011

/datum/thing
	var/price = 60
	better
		proc/test_proc()
			price = parent_type::price + 40