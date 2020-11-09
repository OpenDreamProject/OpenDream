proc/New()
proc/Del()

/list
	proc/Add()
	proc/Remove()

/sound

/client
	var/mob/mob = null
	var/atom/eye = null
	var/ckey = null

	proc/New(TopicData)
		mob = new world.mob(null)

		return mob

	proc/Move(loc, dir)
		mob.Move(loc, dir)

	proc/North()
		Move(locate(mob.x, mob.y + 1, mob.z), 1)

	proc/South()
		Move(locate(mob.x, mob.y - 1, mob.z), 2)

	proc/East()
		Move(locate(mob.x + 1, mob.y, mob.z), 4)

	proc/West()
		Move(locate(mob.x - 1, mob.y, mob.z), 8)

/world
	var/list/contents = list()
	var/tick_lag = 0.5
	var/mob/mob = /mob

/datum

/atom
	var/list/contents = list()
	var/atom/loc = null
	var/x = 0
	var/y = 0
	var/z = 0

	var/icon = null
	var/icon_state = ""
	var/layer = 2.0

/atom/movable
	proc/Move(NewLoc, Dir=0)
		loc = NewLoc

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

	var/client/client = null

	layer = 4.0

	proc/Login()
	proc/Logout()