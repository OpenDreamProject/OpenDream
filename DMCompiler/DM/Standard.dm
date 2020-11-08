proc/New()
proc/Del()

/list
	proc/Add()

/sound

/client

/world
	var/list/contents = list()
	var/tick_lag = 0.5

/datum

/atom
	var/icon = null
	var/icon_state = ""
	var/layer = 2.0

	movable

/area
	parent_type = /atom
	layer = 1.0

/turf
	parent_type = /atom
	layer = 2.0

/obj
	parent_type = /atom/movable
	layer = 3.0

/mob
	parent_type = /atom/movable
	layer = 4.0