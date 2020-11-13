/var/global/world/world = null

proc/New()
proc/Del()

/list
	var/len

	proc/Add()
	proc/Remove()
	proc/Copy()

/sound

/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list()
	var/mob/mob
	var/atom/eye
	var/key
	var/ckey

	proc/New(TopicData)
		mob = new world.mob(null)

		return mob

	proc/Click(object, location, control, params)
		object.Click(location, control, params)

	proc/Move(loc, dir)
		mob.Move(loc, dir)

	proc/North()
		Move(get_step(mob, 1), 1)

	proc/South()
		Move(get_step(mob, 2), 2)

	proc/East()
		Move(get_step(mob, 4), 4)

	proc/West()
		Move(get_step(mob, 8), 8)

/world
	var/list/contents = list()
	var/time = 0
	var/tick_lag = 0.5
	var/mob/mob = /mob

/datum
	var/type

/atom
	parent_type = /datum

	var/list/contents = list()
	var/list/overlays = list()
	var/atom/loc
	var/x = 0
	var/y = 0
	var/z = 0

	var/icon = null
	var/icon_state = ""
	var/layer = 2.0

	proc/Click(location, control, params)

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
	
	var/client/client
	var/key
	var/ckey

	layer = 4.0

	proc/Login()
	proc/Logout()

proc/get_step(atom/Ref, Dir)
	if (Ref == null) return null
	
	var/x = Ref.x
	var/y = Ref.y

	if (Dir & 1) y += 1
	else if (Dir & 2) y -= 1

	if (Dir & 4) x += 1
	else if (Dir & 8) x -= 1

	return locate(max(x, 1), max(y, 1), Ref.z)