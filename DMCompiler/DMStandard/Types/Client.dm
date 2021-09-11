/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list()

	var/atom/statobj
	var/statpanel

	var/mob/mob
	var/atom/eye
	var/perspective = MOB_PERSPECTIVE
	var/view
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0
	var/pixel_w = 0
	var/show_popup_menus = 1 //TODO

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/address
	var/inactivity = 0
	var/key
	var/ckey
	var/connection
	var/computer_id = 0 //TODO

	var/timezone

	proc/New(TopicData)
		view = world.view
		mob = new world.mob(null)

		return mob

	proc/Topic(href, list/href_list, datum/hsrc)
		if (hsrc != null)
			hsrc.Topic(href, href_list)

	proc/Stat()
		if (statobj != null) statobj.Stat()

	proc/Click(atom/object, location, control, params)
		object.Click(location, control, params)

	proc/Move(loc, dir)
		mob.Move(loc, dir)

	proc/North()
		Move(get_step(mob, NORTH), NORTH)

	proc/South()
		Move(get_step(mob, SOUTH), SOUTH)

	proc/East()
		Move(get_step(mob, EAST), EAST)

	proc/West()
		Move(get_step(mob, WEST), WEST)

	proc/Northeast()
		Move(get_step(mob, NORTHEAST), NORTHEAST)

	proc/Southeast()
		Move(get_step(mob, SOUTHEAST), SOUTHEAST)

	proc/Southwest()
		Move(get_step(mob, SOUTHWEST), SOUTHWEST)

	proc/Northwest()
		Move(get_step(mob, NORTHWEST), NORTHWEST)

	proc/Center()
		//TODO: walk(usr, 0)
	
	proc/IsByondMember()
		set opendream_unimplemented = TRUE
		return FALSE
