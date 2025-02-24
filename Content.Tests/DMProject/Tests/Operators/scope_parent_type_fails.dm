// COMPILE ERROR OD0011

/datum/armor
	var/toughness = 100
	reinforced
		proc/test_proc()
			toughness = parent_type::toughness + 50