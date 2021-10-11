/mutable_appearance
	parent_type = /datum

	var/icon = null
	var/icon_state = ""
	var/dir = SOUTH
	var/color = "#FFFFFF"
	var/alpha = 255
	var/layer = 0.0
	var/pixel_x = 0
	var/pixel_y = 0
	var/list/overlays = list()
	var/list/underlays = list()
	
	var/maptext
	var/maptext_x = 0
	var/maptext_y = 0
	var/maptext_height = 32
	var/maptext_width = 32
	
	var/blend_mode = 0
	var/appearance_flags = 0
	
	var/mouse_opacity = 1

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
		else if (!isnull(copy_from))
			CRASH("Invalid arguments for /mutable_appearance/New()")
