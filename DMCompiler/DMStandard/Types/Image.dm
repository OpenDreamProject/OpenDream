/image
	parent_type = /datum

	var/alpha = 255
	var/appearance
	var/appearance_flags = 0
	var/blend_mode = 0
	var/color = "#FFFFFF"
	var/desc = null
	var/gender = "neuter"
	var/infra_luminosity = 0
	var/invisibility = 0
	var/list/filters = list()
	var/layer = FLOAT_LAYER
	var/luminosity = 0
	var/maptext = "i"
	var/maptext_width = 32
	var/maptext_height = 32
	var/maptext_x = 0
	var/maptext_y = 0
	var/mouse_over_pointer = 0
	var/mouse_drag_pointer = 0
	var/mouse_drop_pointer = 1
	var/mouse_drop_zone = 0
	var/mouse_opacity = 1
	var/name = "image"
	var/opacity = 0
	var/list/overlays = list()
	var/override = 1 //TODO
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_w = 0
	var/pixel_z = 0
	var/plane = FLOAT_PLANE
	var/render_source
	var/render_target
	var/suffix
	var/text = "i"
	var/matrix/transform
	var/list/underlays = list()
	var/vis_flags = 0

	var/bound_width
	var/bound_height
	var/name
	var/x
	var/y
	var/z
	var/list/filters = list()
	var/list/vis_contents = list()

	var/dir = SOUTH
	var/icon
	var/icon_state

	var/atom/loc = null

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
