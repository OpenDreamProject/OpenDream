/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list()

	var/mob/mob
	var/key
	var/ckey

	var/atom/eye
	var/view
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0
	var/pixel_w = 0

	proc/New(TopicData)
		view = world.view
		mob = new world.mob(null)

		return mob

	proc/Topic(href, list/href_list, datum/hsrc)
		if (hsrc != null)
			hsrc.Topic(href, href_list)

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