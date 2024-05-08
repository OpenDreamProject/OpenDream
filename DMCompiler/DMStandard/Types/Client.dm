/client
	var/list/verbs = null
	var/list/screen = null
	var/list/images = null
	var/list/vars

	var/atom/statobj
	var/statpanel
	var/default_verb_category = "Commands"

	var/tag
	var/const/type = /client

	var/mob/mob as /mob|null
	var/atom/eye
	var/lazy_eye = 0 as opendream_unimplemented
	var/perspective = MOB_PERSPECTIVE
	var/view
	var/pixel_x = 0 as opendream_unimplemented
	var/pixel_y = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/pixel_w = 0 as opendream_unimplemented
	var/show_popup_menus = 1 as opendream_unimplemented
	var/show_verb_panel = 1 as opendream_unimplemented

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/address
	var/inactivity = 0 as opendream_unimplemented
	var/key as text|null
	var/ckey as text|null
	var/connection
	var/computer_id = 0
	var/tick_lag = 0 as opendream_unimplemented

	var/timezone

	var/script as opendream_unimplemented
	var/color = 0 as opendream_unimplemented
	var/control_freak as opendream_unimplemented
	var/mouse_pointer_icon as opendream_unimplemented
	var/preload_rsc = 1 as opendream_unimplemented
	var/fps = 0 as opendream_unimplemented
	var/dir = NORTH as opendream_unimplemented
	var/gender = "neuter" as opendream_unimplemented
	var/glide_size as opendream_unimplemented
	var/virtual_eye as opendream_unimplemented

	proc/New(TopicData)
		// Search every mob for one with our ckey
		// TODO: This /mob|mob thing is kinda silly huh?
		for (var/mob/M as /mob|mob in world)
			if (M.key == key)
				mob = M
				break

		if (mob == null) // No existing mob, create a default one
			mob = new world.mob(locate(1,1,1)) // TODO: Find nearest non-dense turf

		eye = mob
		statobj = mob
		return mob

	proc/Del()
		set opendream_unimplemented = TRUE

	proc/Topic(href, list/href_list, datum/hsrc)
		if (hsrc != null)
			hsrc.Topic(href, href_list)

	proc/Stat()
		if (istype(statobj, /atom))
			statobj.Stat()

	proc/Command(command as command_text)
		set opendream_unimplemented = TRUE

	proc/Import(Query)
		set opendream_unimplemented = TRUE
	proc/Export(file)
		set opendream_unimplemented = TRUE
	proc/AllowUpload(filename, filelength)
		set opendream_unimplemented = TRUE
		return TRUE

	proc/SoundQuery()
		set opendream_unimplemented = TRUE
	proc/MeasureText(text, style, width=0)
		set opendream_unimplemented = TRUE

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

	proc/Click(atom/object, location, control, params)
		object.Click(location, control, params)

	proc/DblClick(atom/object, location, control, params)
		set opendream_unimplemented = TRUE
		object.DblClick(location,control,params)

	proc/MouseDown(atom/object, location, control, params)
		set opendream_unimplemented = TRUE
		object.MouseDown(location, control, params)

	proc/MouseDrag(atom/src_object,over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = TRUE
		src_object.MouseDrag(over_object,src_location,over_location,src_control,over_control,params)

	proc/MouseDrop(atom/src_object,over_object,src_location,over_location,src_control,over_control,params)
		src_object.MouseDrop(over_object,src_location,over_location,src_control,over_control,params)

	proc/MouseEntered(atom/object,location,control,params)
		set opendream_unimplemented = TRUE
		object.MouseEntered(location,control,params)

	proc/MouseExited(atom/object,location,control,params)
		set opendream_unimplemented = TRUE
		object.MouseExited(location,control,params)

	proc/MouseMove(atom/object,location,control,params)
		set opendream_unimplemented = TRUE
		object.MouseMove(location,control,params)

	proc/MouseUp(atom/object,location,control,params)
		set opendream_unimplemented = TRUE
		object.MouseUp(location,control,params)

	proc/MouseWheel(atom/object,delta_x,delta_y,location,control,params)
		set opendream_unimplemented = TRUE
		object.MouseWheel(delta_x,delta_y,location,control,params)

	proc/IsByondMember()
		set opendream_unimplemented = TRUE
		return FALSE
	proc/CheckPassport(passport_identifier)
		set opendream_unimplemented = TRUE
	proc/SendPage(msg, recipient, options)
		set opendream_unimplemented = TRUE
	proc/GetAPI(Api, Name)
		set opendream_unimplemented = TRUE
	proc/SetAPI(Api, Key, Value)
		set opendream_unimplemented = TRUE
	proc/RenderIcon(object)
		set opendream_unimplemented = TRUE
		return object