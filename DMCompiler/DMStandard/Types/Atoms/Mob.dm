/mob
	parent_type = /atom/movable

	var/client/client as /client|null
	var/key as text|null
	var/tmp/ckey as text|null

	var/tmp/list/group as opendream_unimplemented

	var/see_invisible = 0 as num
	var/see_infrared = 0 as num|opendream_unimplemented
	var/sight = 0 as num
	var/see_in_dark = 2 as num|opendream_unimplemented

	density = TRUE
	layer = MOB_LAYER

	proc/Login()

	proc/Logout()
