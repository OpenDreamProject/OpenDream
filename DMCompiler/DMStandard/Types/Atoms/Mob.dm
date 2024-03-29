/mob
	parent_type = /atom/movable

	var/client/client
	var/key
	var/tmp/ckey

	var/tmp/list/group as opendream_unimplemented

	var/see_invisible = 0
	var/see_infrared = 0 as opendream_unimplemented
	var/sight = 0
	var/see_in_dark = 2 as opendream_unimplemented

	density = TRUE
	layer = MOB_LAYER

	proc/Login()

	proc/Logout()
