/mutable_appearance
	parent_type = /image

	var/animate_movement = 1 as opendream_unimplemented
	var/screen_loc as opendream_unimplemented

	New(var/datum/copy_from)
		if (istype(copy_from, /mutable_appearance))
			var/mutable_appearance/appearance = copy_from

			src.icon = appearance.icon
			src.icon_state = appearance.icon_state
			src.dir = appearance.dir
			src.color = appearance.color
			src.alpha = appearance.alpha
			src.layer = appearance.layer
			src.pixel_x = appearance.pixel_x
			src.pixel_y = appearance.pixel_y
		else if (istype(copy_from, /image))
			var/image/image = copy_from

			src.icon = image.icon
			src.icon_state = image.icon_state
			src.dir = image.dir
			src.color = image.color
			src.alpha = image.alpha
			src.layer = image.layer
			src.pixel_x = image.pixel_x
			src.pixel_y = image.pixel_y
		else if (isfile(copy_from))
			src.icon = copy_from
		else if (!isnull(copy_from))
			CRASH("Invalid arguments for /mutable_appearance/New()")
