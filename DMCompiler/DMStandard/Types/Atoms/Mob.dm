/mob
	parent_type = /atom/movable

	var/client/client
	var/key as text|null
	var/tmp/ckey as text|null

	var/tmp/list/group as opendream_unimplemented

	var/see_invisible = 0
	var/see_infrared = 0 as opendream_unimplemented
	var/sight = 0
	var/see_in_dark = 2 as opendream_unimplemented

	density = TRUE
	layer = MOB_LAYER

	proc/Login()
		if(!loc)
			var/turf/fallback_spawn = locate(1, 1, 1) // TODO: Find nearest non-dense turf
			if(fallback_spawn)
				loc = fallback_spawn

	proc/Logout()
		del client
