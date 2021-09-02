/image
	parent_type = /datum

	var/icon = null
	var/icon_state = null
	var/list/overlays = list()
	var/atom/loc = null
	var/layer = FLOAT_LAYER
	var/dir = SOUTH
	var/pixel_x = 0
	var/pixel_y = 0
	var/color = "#FFFFFF"
	var/alpha = 255

	var/blend_mode = 0
	var/matrix/transform
	var/override = 1 //TODO

	New(icon, loc, icon_state, layer, dir)
		src.icon = icon
		if (!istext(loc))
			if (loc != null) src.loc = loc
			if (icon_state != null) src.icon_state = icon_state
			if (layer != null) src.layer = layer
			if (dir != null) src.dir = dir
		else
			if (loc != null) src.icon_state = loc
			if (icon_state != null) src.layer = icon_state
			if (layer != null) src.dir = layer
			if (dir != null) src.dir = dir
