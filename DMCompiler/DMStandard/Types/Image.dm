/image
	parent_type = /datum

	var/alpha = 255
	var/appearance as opendream_unimplemented
	var/appearance_flags = 0
	var/blend_mode = 0
	var/color = "#FFFFFF"
	var/desc = null
	var/gender = "neuter" as opendream_unimplemented
	var/infra_luminosity = 0 as opendream_unimplemented
	var/invisibility = 0 as opendream_unimplemented
	var/list/filters = list()
	var/layer = FLOAT_LAYER
	var/luminosity = 0 as opendream_unimplemented
	var/maptext = "i" as opendream_unimplemented
	var/maptext_width = 32 as opendream_unimplemented
	var/maptext_height = 32 as opendream_unimplemented
	var/maptext_x = 0 as opendream_unimplemented
	var/maptext_y = 0 as opendream_unimplemented
	var/mouse_over_pointer = 0 as opendream_unimplemented
	var/mouse_drag_pointer = 0 as opendream_unimplemented
	var/mouse_drop_pointer = 1 as opendream_unimplemented
	var/mouse_drop_zone = 0 as opendream_unimplemented
	var/mouse_opacity = 1
	var/name = "image"
	var/opacity = 0 as opendream_unimplemented
	var/list/overlays = list()
	var/override = 1 as opendream_unimplemented
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_w = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/plane = FLOAT_PLANE
	var/render_source
	var/render_target
	var/suffix as opendream_unimplemented
	var/text = "i" as opendream_unimplemented
	var/matrix/transform
	var/list/underlays = list()
	var/vis_flags = 0 as opendream_unimplemented

	var/bound_width as opendream_unimplemented
	var/bound_height as opendream_unimplemented
	var/x
	var/y
	var/z
	var/list/vis_contents = list() as opendream_unimplemented

	var/dir = SOUTH
	var/icon
	var/icon_state

	var/atom/loc = null

	// The ref does not mention the pixel_x and pixel_y args...
	New(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
		if (icon != null) src.icon = icon
		if (!istext(loc))
			if (loc != null) src.loc = loc
			if (icon_state != null) src.icon_state = icon_state
			if (layer != null) src.layer = layer
			if (dir != null) src.dir = dir
			if (pixel_x != null) src.pixel_x = pixel_x
			if (pixel_y != null) src.pixel_y = pixel_y
		else
			if (loc != null) src.icon_state = loc
			if (icon_state != null) src.layer = icon_state
			if (layer != null) src.dir = layer
			if (dir != null) src.pixel_x = dir
			if (pixel_x != null) src.pixel_y = pixel_x
