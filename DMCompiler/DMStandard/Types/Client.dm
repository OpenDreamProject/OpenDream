/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list() as opendream_unimplemented

	var/atom/statobj
	var/statpanel
	
	var/tag
	var/type = /client

	var/mob/mob
	var/atom/eye
	var/perspective = MOB_PERSPECTIVE
	var/view
	var/pixel_x = 0 as opendream_unimplemented
	var/pixel_y = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/pixel_w = 0 as opendream_unimplemented
	var/show_popup_menus = 1 as opendream_unimplemented

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/address
	var/inactivity = 0 as opendream_unimplemented
	var/key
	var/ckey
	var/connection
	var/computer_id = 0 as opendream_unimplemented

	var/timezone

	var/color = 0 as opendream_unimplemented
	var/control_freak as opendream_unimplemented
	var/mouse_pointer_icon as opendream_unimplemented
	var/preload_rsc = 1 as opendream_unimplemented
	var/fps = 0 as opendream_unimplemented
	var/dir = NORTH as opendream_unimplemented
	var/gender = "neuter" as opendream_unimplemented
	var/glide_size as opendream_unimplemented
	proc/SoundQuery()
		set opendream_unimplemented = TRUE
	proc/Export(file)
		set opendream_unimplemented = TRUE
	proc/MeasureText(text, style, width=0)
		set opendream_unimplemented = TRUE
	
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
