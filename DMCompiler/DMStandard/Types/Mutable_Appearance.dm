/mutable_appearance
	parent_type = /datum

	var/icon = null
	var/icon_state = ""
	var/color = "#FFFFFF"
	var/alpha = 255
	var/layer = 0.0
	var/pixel_x = 0
	var/pixel_y = 0

	New(mutable_appearance/appearance)
		if (istype(appearance, /mutable_appearance))
			src.icon = appearance.icon
			src.icon_state = appearance.icon_state
			src.color = appearance.color
			src.alpha = appearance.alpha
			src.layer = appearance.layer
			src.pixel_x = appearance.pixel_x
			src.pixel_y = appearance.pixel_y
		else if (!isnull(appearance))
			CRASH("Invalid arguments for /mutable_appearance/New()")